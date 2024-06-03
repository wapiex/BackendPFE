using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using testloggg.models;

namespace WebApplication5.Controllers
{
    public class AuthController : Controller
    {
        private readonly string connectionString = @"Data Source=DESKTOP-E36VL9V\SQLEXPRESS;Initial Catalog=log; Integrated Security=true;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

        [HttpPost("api/auth")]
        public async Task<IActionResult> AuthenticateAsync([FromBody] Login login)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    var authSql = "SELECT UtilisateurID, statut FROM Utilisateur WHERE login = @Username AND mdp = @Password";
                    var authCommand = new SqlCommand(authSql, connection);
                    authCommand.Parameters.AddWithValue("@Username", login.Username);
                    authCommand.Parameters.AddWithValue("@Password", login.Password);

                    using (var reader = await authCommand.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var utilisateurID = reader["UtilisateurID"].ToString();
                            var status = reader["statut"].ToString();
                            var accounts = await GetAccountsInfo(utilisateurID);
                            if (accounts == null || !accounts.Any())
                            {
                                return NotFound(new { Message = "No accounts found for this user" });
                            }

                            var userName = accounts.FirstOrDefault()?.Nom;
                            var cards = new List<dynamic>();
                            foreach (var account in accounts)
                            {
                                var accountCards = await GetCardsInfo(account.numcpt);
                                cards.AddRange(accountCards);
                            }

                            return Ok(new { Message = "Authentication successful", User = userName, Status = status, UtilisateurID = utilisateurID, Accounts = accounts, Cards = cards });
                        }
                        else
                        {
                            return NotFound(new { Message = "Invalid username or password" });
                        }
                    }
                }
                catch (Exception e)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred accessing the database", Exception = e.Message });
                }
            }
        }


        private async Task<List<dynamic>> GetAccountsInfo(string utilisateurID)
        {
            var accounts = new List<dynamic>();
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"
                    SELECT c.numcpt, c.nom, c.prenom, c.typecpt, c.datecreation, c.statut, c.solde
                    FROM Comptes c
                    JOIN UtilisateurComptes uc ON c.numcpt = uc.numcpt
                    WHERE uc.UtilisateurID = @UtilisateurID";
                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UtilisateurID", utilisateurID);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        accounts.Add(new
                        {
                            numcpt = reader["numcpt"].ToString(),
                            Nom = reader["nom"].ToString(),
                            Prenom = reader["prenom"].ToString(),
                            TypeCpt = reader["typecpt"].ToString(),
                            DateCreation = reader["datecreation"].ToString(),
                            Statut = reader["statut"].ToString(),
                            Solde = reader["solde"].ToString()
                        });
                    }
                }
            }
            return accounts;
        }

        private async Task<List<dynamic>> GetCardsInfo(string accountNumber)
        {
            var cards = new List<dynamic>();
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT numcarte, nom, prenom, typecarte, datecreation, dateexpiration, statut FROM Cartes WHERE numcpt = @AccountNumber";
                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@AccountNumber", accountNumber);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        cards.Add(new
                        {
                            NumCarte = reader["numcarte"].ToString(),
                            Nom = reader["nom"].ToString(),
                            Prenom = reader["prenom"].ToString(),
                            TypeCarte = reader["typecarte"].ToString(),
                            DateCreation = reader["datecreation"].ToString(),
                            DateExpiration = reader["dateexpiration"].ToString(),
                            Statut = reader["statut"].ToString()
                        });
                    }
                }
            }
            return cards;
        }


        [HttpPost("api/submitCardRequest")]
        public async Task<IActionResult> SubmitCardRequest([FromBody] CardRequest cardRequest)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    var query = @"
                        INSERT INTO DemandesCarte (
                            compteId, typeCarte, nom, prenoms, profession, adresse, ville, codePostal, telephone, mobile,
                            typeIdentite, numeroIdentite, dateDelivranceIdentite, revenuMensuelNet, soldeCompte, soldeAVA,
                            mouvementAnnuel, cotePersonalisation, plafondHebdoDAB, plafondHebdoTPE, dateDemande, statusDemande
                        ) VALUES (
                            @CompteId, @TypeCarte, @Nom, @Prenoms, @Profession, @Adresse, @Ville, @CodePostal, @Telephone, @Mobile,
                            @TypeIdentite, @NumeroIdentite, @DateDelivranceIdentite, @RevenuMensuelNet, @SoldeCompte, @SoldeAVA,
                            @MouvementAnnuel, @CotePersonalisation, @PlafondHebdoDAB, @PlafondHebdoTPE, GETDATE(), 'En attente'
                        )";

                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@CompteId", cardRequest.CompteId);
                    command.Parameters.AddWithValue("@TypeCarte", cardRequest.TypeCarte);
                    command.Parameters.AddWithValue("@Nom", cardRequest.Nom);
                    command.Parameters.AddWithValue("@Prenoms", cardRequest.Prenoms);
                    command.Parameters.AddWithValue("@Profession", cardRequest.Profession);
                    command.Parameters.AddWithValue("@Adresse", cardRequest.Adresse);
                    command.Parameters.AddWithValue("@Ville", cardRequest.Ville);
                    command.Parameters.AddWithValue("@CodePostal", cardRequest.CodePostal);
                    command.Parameters.AddWithValue("@Telephone", cardRequest.Telephone);
                    command.Parameters.AddWithValue("@Mobile", cardRequest.Mobile);
                    command.Parameters.AddWithValue("@TypeIdentite", cardRequest.TypeIdentite);
                    command.Parameters.AddWithValue("@NumeroIdentite", cardRequest.NumeroIdentite);
                    command.Parameters.AddWithValue("@DateDelivranceIdentite", cardRequest.DateDelivranceIdentite);
                    command.Parameters.AddWithValue("@RevenuMensuelNet", cardRequest.RevenuMensuelNet);
                    command.Parameters.AddWithValue("@SoldeCompte", cardRequest.SoldeCompte);
                    command.Parameters.AddWithValue("@SoldeAVA", cardRequest.SoldeAVA);
                    command.Parameters.AddWithValue("@MouvementAnnuel", cardRequest.MouvementAnnuel);
                    command.Parameters.AddWithValue("@CotePersonalisation", cardRequest.CotePersonalisation);
                    command.Parameters.AddWithValue("@PlafondHebdoDAB", cardRequest.PlafondHebdoDAB);
                    command.Parameters.AddWithValue("@PlafondHebdoTPE", cardRequest.PlafondHebdoTPE);

                    int result = await command.ExecuteNonQueryAsync();
                    if (result > 0)
                    {
                        return Ok(new { message = "Demande de carte soumise avec succès." });
                    }
                    else
                    {
                        return BadRequest(new { message = "Erreur lors de la soumission de la demande." });
                    }
                }
                catch (Exception e)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred accessing the database", Exception = e.Message });
                }
            }
        }

        [HttpPost("api/initiateVirement")]
        public async Task<IActionResult> InitiateVirement([FromBody] VirementRequest virementRequest)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    var query = @"
            INSERT INTO Virements (
                NumCpt, Montant, TypeVirement, Validation, DateInitiation, CompteBeneficiaire, Statut, UtilisateurID, Motif
            ) VALUES (
                @NumCpt, @Montant, @TypeVirement, @Validation, @DateInitiation, @CompteBeneficiaire, @Statut, @UtilisateurID, @Motif
            )";

                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@NumCpt", virementRequest.NumCpt);
                    command.Parameters.AddWithValue("@Montant", virementRequest.Montant);
                    command.Parameters.AddWithValue("@TypeVirement", virementRequest.TypeVirement);
                    command.Parameters.AddWithValue("@Validation", false); // Initialisé comme non validé
                    command.Parameters.AddWithValue("@DateInitiation", virementRequest.DateInitiation);
                    command.Parameters.AddWithValue("@CompteBeneficiaire", virementRequest.CompteBeneficiaire);
                    command.Parameters.AddWithValue("@Statut", "Initialisé");
                    command.Parameters.AddWithValue("@UtilisateurID", virementRequest.UtilisateurID);
                    command.Parameters.AddWithValue("@Motif", virementRequest.Motif); // Ajout du motif

                    int result = await command.ExecuteNonQueryAsync();
                    if (result > 0)
                    {
                        return Ok(new { message = "Virement initialisé avec succès." });
                    }
                    else
                    {
                        return BadRequest(new { message = "Erreur lors de l'initiation du virement." });
                    }
                }
                catch (Exception e)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred accessing the database", Exception = e.Message });
                }
            }
        }
        [HttpPost("api/initiateMultipleVirement")]
        public async Task<IActionResult> InitiateMultipleVirement(IFormCollection formData)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var typeVirement = formData["typeVirement"].FirstOrDefault();
                    var numCpt = formData["numCpt"].FirstOrDefault();
                    var utilisateurID = formData["utilisateurID"].FirstOrDefault();

                    if (string.IsNullOrEmpty(typeVirement) || string.IsNullOrEmpty(numCpt) || string.IsNullOrEmpty(utilisateurID))
                    {
                        return BadRequest(new { message = "Tous les champs du formulaire sont obligatoires." });
                    }

                    List<Beneficiary> beneficiaries = new List<Beneficiary>();

                    var file = formData.Files.FirstOrDefault();
                    if (file != null && file.Length > 0)
                    {
                        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                        {
                            HeaderValidated = null,
                            MissingFieldFound = null
                        };
                        using (var reader = new StreamReader(file.OpenReadStream()))
                        using (var csv = new CsvReader(reader, config))
                        {
                            beneficiaries = csv.GetRecords<Beneficiary>().ToList();
                        }
                    }

                    if (beneficiaries.Count == 0)
                    {
                        return BadRequest(new { message = "Aucun bénéficiaire trouvé dans le fichier CSV." });
                    }

                    foreach (var beneficiary in beneficiaries)
                    {
                        if (beneficiary.Montant == 0 || string.IsNullOrEmpty(beneficiary.CompteBeneficiaire) ||
                            string.IsNullOrEmpty(beneficiary.NomPrenom) || string.IsNullOrEmpty(beneficiary.RIB) ||
                            string.IsNullOrEmpty(beneficiary.Motif) || string.IsNullOrEmpty(beneficiary.Reference))
                        {
                            return BadRequest(new { message = "Les données des bénéficiaires sont incomplètes." });
                        }

                        var query = @"
                INSERT INTO Virements (
                    NumCpt, Montant, TypeVirement, Validation, DateInitiation, CompteBeneficiaire, Statut, UtilisateurID, Motif
                ) VALUES (
                    @NumCpt, @Montant, @TypeVirement, @Validation, @DateInitiation, @CompteBeneficiaire, @Statut, @UtilisateurID, @Motif
                )";

                        var command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@NumCpt", numCpt);
                        command.Parameters.AddWithValue("@Montant", beneficiary.Montant);
                        command.Parameters.AddWithValue("@TypeVirement", typeVirement);
                        command.Parameters.AddWithValue("@Validation", false); // Initialisé comme non validé
                        command.Parameters.AddWithValue("@DateInitiation", DateTime.Now);
                        command.Parameters.AddWithValue("@CompteBeneficiaire", beneficiary.CompteBeneficiaire);
                        command.Parameters.AddWithValue("@Statut", "Initialisé");
                        command.Parameters.AddWithValue("@UtilisateurID", utilisateurID);
                        command.Parameters.AddWithValue("@Motif", beneficiary.Motif); // Ajout du motif

                        int result = await command.ExecuteNonQueryAsync();
                        if (result <= 0)
                        {
                            return BadRequest(new { message = "Erreur lors de l'initiation du virement pour un bénéficiaire." });
                        }
                    }

                    return Ok(new { message = "Virements multiples initialisés avec succès." });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Erreur : " + e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred accessing the database", Exception = e.Message });
            }
        }



        [HttpGet("api/virements/validate/{utilisateurID}")]
        public async Task<IActionResult> GetVirementsToValidateAsync(int utilisateurID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    var query = @"
                SELECT v.Id, v.NumCpt, v.Montant, v.TypeVirement, v.Validation, v.DateInitiation, v.DateValidation, v.CompteBeneficiaire, v.Statut, v.UtilisateurID, v.Motif
                FROM Virements v
                JOIN UtilisateurComptes uc ON v.NumCpt = uc.numcpt
                WHERE uc.UtilisateurID = @UtilisateurID AND v.Validation = 0";
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@UtilisateurID", utilisateurID);

                    var virements = new List<dynamic>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            virements.Add(new
                            {
                                Id = reader["Id"],
                                NumCpt = reader["NumCpt"],
                                Montant = reader["Montant"],
                                TypeVirement = reader["TypeVirement"],
                                Validation = reader["Validation"],
                                DateInitiation = reader["DateInitiation"],
                                DateValidation = reader["DateValidation"],
                                CompteBeneficiaire = reader["CompteBeneficiaire"],
                                Statut = reader["Statut"],
                                UtilisateurID = reader["UtilisateurID"],
                                Motif = reader["Motif"]
                            });
                        }
                    }

                    return Ok(virements);
                }
                catch (Exception e)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred accessing the database", Exception = e.Message });
                }
            }
        }


        [HttpPost("api/validateVirement")]
        public async Task<IActionResult> ValidateVirement([FromBody] VirementValidationRequest validationRequest)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    var query = "UPDATE Virements SET Validation = @Validation, DateValidation = @DateValidation, Statut = @Statut WHERE Id = @VirementId";

                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@VirementId", validationRequest.VirementId);
                    command.Parameters.AddWithValue("@Validation", true); // Validation du virement
                    command.Parameters.AddWithValue("@DateValidation", DateTime.Now);
                    command.Parameters.AddWithValue("@Statut", "Validé");

                    int result = await command.ExecuteNonQueryAsync();
                    if (result > 0)
                    {
                        // Récupérer les détails du virement validé
                        var selectQuery = "SELECT Id, NumCpt, Montant, TypeVirement, Validation, DateInitiation, DateValidation, CompteBeneficiaire, Statut, UtilisateurID, Motif FROM Virements WHERE Id = @VirementId";
                        var selectCommand = new SqlCommand(selectQuery, connection);
                        selectCommand.Parameters.AddWithValue("@VirementId", validationRequest.VirementId);

                        using (var reader = await selectCommand.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var virement = new
                                {
                                    Id = reader["Id"],
                                    NumCpt = reader["NumCpt"],
                                    Montant = reader["Montant"],
                                    TypeVirement = reader["TypeVirement"],
                                    Validation = reader["Validation"],
                                    DateInitiation = reader["DateInitiation"],
                                    DateValidation = reader["DateValidation"],
                                    CompteBeneficiaire = reader["CompteBeneficiaire"],
                                    Statut = reader["Statut"],
                                    UtilisateurID = reader["UtilisateurID"],
                                    Motif = reader["Motif"]
                                };
                                return Ok(virement);
                            }
                        }
                    }
                    return BadRequest(new { message = "Erreur lors de la validation du virement." });
                }
                catch (Exception e)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred accessing the database", Exception = e.Message });
                }
            }
        }

        [HttpPost("api/cancelVirement")]
        public async Task<IActionResult> CancelVirement([FromBody] VirementValidationRequest validationRequest)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    var query = "UPDATE Virements SET Statut = @Statut WHERE Id = @VirementId";

                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Statut", "Annulé");
                    command.Parameters.AddWithValue("@VirementId", validationRequest.VirementId);

                    int result = await command.ExecuteNonQueryAsync();
                    if (result > 0)
                    {
                        // Récupérer les détails du virement annulé
                        var selectQuery = "SELECT Id, NumCpt, Montant, TypeVirement, Validation, DateInitiation, DateValidation, CompteBeneficiaire, Statut, UtilisateurID, Motif FROM Virements WHERE Id = @VirementId";
                        var selectCommand = new SqlCommand(selectQuery, connection);
                        selectCommand.Parameters.AddWithValue("@VirementId", validationRequest.VirementId);

                        using (var reader = await selectCommand.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var virement = new
                                {
                                    Id = reader["Id"],
                                    NumCpt = reader["NumCpt"],
                                    Montant = reader["Montant"],
                                    TypeVirement = reader["TypeVirement"],
                                    Validation = reader["Validation"],
                                    DateInitiation = reader["DateInitiation"],
                                    DateValidation = reader["DateValidation"],
                                    CompteBeneficiaire = reader["CompteBeneficiaire"],
                                    Statut = reader["Statut"],
                                    UtilisateurID = reader["UtilisateurID"],
                                    Motif = reader["Motif"]
                                };
                                return Ok(virement);
                            }
                        }
                    }
                    return BadRequest(new { message = "Erreur lors de l'annulation du virement." });
                }
                catch (Exception e)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred accessing the database", Exception = e.Message });
                }
            }
        }
        [HttpGet("api/virementsByDate")]
        public async Task<IActionResult> GetVirementsByDateAsync([FromQuery] string startDate, [FromQuery] string endDate, [FromQuery] int utilisateurID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    var query = @"
                SELECT v.Id, v.NumCpt, v.Montant, v.TypeVirement, v.Validation, v.DateInitiation, v.DateValidation, v.CompteBeneficiaire, v.Statut, v.Motif
                FROM Virements v
                JOIN UtilisateurComptes uc ON v.NumCpt = uc.numcpt
                WHERE uc.UtilisateurID = @UtilisateurID
                AND v.DateInitiation BETWEEN @StartDate AND @EndDate";
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@UtilisateurID", utilisateurID);
                    command.Parameters.AddWithValue("@StartDate", DateTime.Parse(startDate));
                    command.Parameters.AddWithValue("@EndDate", DateTime.Parse(endDate));

                    var virements = new List<dynamic>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            virements.Add(new
                            {
                                Id = reader["Id"],
                                NumCpt = reader["NumCpt"],
                                Montant = reader["Montant"],
                                TypeVirement = reader["TypeVirement"],
                                Validation = reader["Validation"],
                                DateInitiation = reader["DateInitiation"],
                                DateValidation = reader["DateValidation"],
                                CompteBeneficiaire = reader["CompteBeneficiaire"],
                                Statut = reader["Statut"],
                                Motif = reader["Motif"]
                            });
                        }
                    }

                    return Ok(virements);
                }
                catch (Exception e)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred accessing the database", Exception = e.Message });
                }
            }
        }

        [HttpGet("api/virements/valides")]
        public async Task<IActionResult> GetValidVirementsAsync([FromQuery] int utilisateurID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    var query = @"
                SELECT v.Id, v.NumCpt, v.Montant, v.TypeVirement, v.Validation, v.DateInitiation, v.DateValidation, v.CompteBeneficiaire, v.Statut
                FROM Virements v
                JOIN UtilisateurComptes uc ON v.NumCpt = uc.numcpt
                WHERE uc.UtilisateurID = @UtilisateurID AND v.Statut = 'Validé'";
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@UtilisateurID", utilisateurID);

                    var virements = new List<dynamic>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            virements.Add(new
                            {
                                Id = reader["Id"],
                                NumCpt = reader["NumCpt"],
                                Montant = reader["Montant"],
                                TypeVirement = reader["TypeVirement"],
                                Validation = reader["Validation"],
                                DateInitiation = reader["DateInitiation"],
                                DateValidation = reader["DateValidation"],
                                CompteBeneficiaire = reader["CompteBeneficiaire"],
                                Statut = reader["Statut"]
                            });
                        }
                    }

                    return Ok(virements);
                }
                catch (Exception e)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred accessing the database", Exception = e.Message });
                }
            }
        }

        [HttpGet("api/virements/annules")]
        public async Task<IActionResult> GetCancelledVirementsAsync([FromQuery] int utilisateurID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    var query = @"
                SELECT v.Id, v.NumCpt, v.Montant, v.TypeVirement, v.Validation, v.DateInitiation, v.DateValidation, v.CompteBeneficiaire, v.Statut
                FROM Virements v
                JOIN UtilisateurComptes uc ON v.NumCpt = uc.numcpt
                WHERE uc.UtilisateurID = @UtilisateurID AND v.Statut = 'Annulé'";
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@UtilisateurID", utilisateurID);

                    var virements = new List<dynamic>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            virements.Add(new
                            {
                                Id = reader["Id"],
                                NumCpt = reader["NumCpt"],
                                Montant = reader["Montant"],
                                TypeVirement = reader["TypeVirement"],
                                Validation = reader["Validation"],
                                DateInitiation = reader["DateInitiation"],
                                DateValidation = reader["DateValidation"],
                                CompteBeneficiaire = reader["CompteBeneficiaire"],
                                Statut = reader["Statut"]
                            });
                        }
                    }

                    return Ok(virements);
                }
                catch (Exception e)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred accessing the database", Exception = e.Message });
                }
            }
        }
        [HttpGet("api/virements/stats")]
        public async Task<IActionResult> GetVirementStatsAsync([FromQuery] int utilisateurID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var queryValid = @"
            SELECT COUNT(*) 
            FROM Virements v
            JOIN UtilisateurComptes uc ON v.NumCpt = uc.numcpt
            WHERE uc.UtilisateurID = @UtilisateurID AND v.Statut = 'Validé'";

                    var queryAnnule = @"
            SELECT COUNT(*) 
            FROM Virements v
            JOIN UtilisateurComptes uc ON v.NumCpt = uc.numcpt
            WHERE uc.UtilisateurID = @UtilisateurID AND v.Statut = 'Annulé'";

                    var commandValid = new SqlCommand(queryValid, connection);
                    var commandAnnule = new SqlCommand(queryAnnule, connection);
                    commandValid.Parameters.AddWithValue("@UtilisateurID", utilisateurID);
                    commandAnnule.Parameters.AddWithValue("@UtilisateurID", utilisateurID);

                    var validCount = (int)await commandValid.ExecuteScalarAsync();
                    var annuleCount = (int)await commandAnnule.ExecuteScalarAsync();

                    return Ok(new { valides = validCount, annules = annuleCount });
                }
                catch (Exception e)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred accessing the database", Exception = e.Message });
                }
            }
        }


    }
}
