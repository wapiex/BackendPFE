/*using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
                    connection.Open();
                    var authSql = "SELECT numcpt, statut FROM login WHERE login = @Username AND mdp = @Password";
                    var authCommand = new SqlCommand(authSql, connection);
                    authCommand.Parameters.AddWithValue("@Username", login.Username);
                    authCommand.Parameters.AddWithValue("@Password", login.Password);

                    using (var reader = authCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var accountNumber = reader["numcpt"].ToString();
                            var status = reader["statut"].ToString();
                            var accountInfo = await GetAccountInfo(accountNumber);
                            if (accountInfo == null)
                            {
                                return NotFound(new { Message = "Account not found" });
                            }

                            var cards = await GetCardsInfo(accountNumber);
                            return Ok(new { Message = "Authentication successful", User = login.Username, Status = status, Account = accountInfo, Cards = cards });
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

        private async Task<dynamic> GetAccountInfo(string accountNumber)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var sql = "SELECT nom, prenom, typecpt, datecreation, statut, solde FROM Comptes WHERE numcpt = @AccountNumber";
                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@AccountNumber", accountNumber);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new
                        {
                            Nom = reader["nom"].ToString(),
                            Prenom = reader["prenom"].ToString(),
                            TypeCpt = reader["typecpt"].ToString(),
                            DateCreation = reader["datecreation"].ToString(),
                            Statut = reader["statut"].ToString(),
                            Solde = reader["solde"].ToString()
                        };
                    }
                }
            }
            return null;
        }

        private async Task<List<dynamic>> GetCardsInfo(string accountNumber)
        {
            var cards = new List<dynamic>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var sql = "SELECT numcarte, nom, prenom, typecarte, datecreation, dateexpiration, statut FROM Cartes WHERE numcpt = @AccountNumber";
                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@AccountNumber", accountNumber);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
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
                    connection.Open();
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

                    int result = command.ExecuteNonQuery();
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
    }
}*/


using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
                    connection.Open();
                    var authSql = "SELECT numcpt, statut FROM login WHERE login = @Username AND mdp = @Password";
                    var authCommand = new SqlCommand(authSql, connection);
                    authCommand.Parameters.AddWithValue("@Username", login.Username);
                    authCommand.Parameters.AddWithValue("@Password", login.Password);

                    using (var reader = authCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var accountNumber = reader["numcpt"].ToString();
                            var status = reader["statut"].ToString();
                            var accountInfo = await GetAccountInfo(accountNumber);
                            if (accountInfo == null)
                            {
                                return NotFound(new { Message = "Account not found" });
                            }

                            var cards = await GetCardsInfo(accountNumber);
                            return Ok(new { Message = "Authentication successful", User = accountInfo.Nom, Status = status, Account = accountInfo, Cards = cards });
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

        private async Task<dynamic> GetAccountInfo(string accountNumber)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var sql = "SELECT nom, prenom, typecpt, datecreation, statut, solde FROM Comptes WHERE numcpt = @AccountNumber";
                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@AccountNumber", accountNumber);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new
                        {
                            Nom = reader["nom"].ToString(),
                            Prenom = reader["prenom"].ToString(),
                            TypeCpt = reader["typecpt"].ToString(),
                            DateCreation = reader["datecreation"].ToString(),
                            Statut = reader["statut"].ToString(),
                            Solde = reader["solde"].ToString()
                        };
                    }
                }
            }
            return null;
        }

        private async Task<List<dynamic>> GetCardsInfo(string accountNumber)
        {
            var cards = new List<dynamic>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var sql = "SELECT numcarte, nom, prenom, typecarte, datecreation, dateexpiration, statut FROM Cartes WHERE numcpt = @AccountNumber";
                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@AccountNumber", accountNumber);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
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
                    connection.Open();
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

                    int result = command.ExecuteNonQuery();
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
    }
}

