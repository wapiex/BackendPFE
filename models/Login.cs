namespace testloggg.models
{
    public class Login
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public  string Password { get; set; }
        public string Status { get; set; }
    }

    public class CardRequest
    {
        public int CompteId { get; set; }
        public string TypeCarte { get; set; }
        public string Nom { get; set; }
        public string Prenoms { get; set; }
        public string Profession { get; set; }
        public string Adresse { get; set; }
        public string Ville { get; set; }
        public string CodePostal { get; set; }
        public string Telephone { get; set; }
        public string Mobile { get; set; }
        public string TypeIdentite { get; set; }
        public string NumeroIdentite { get; set; }
        public DateTime DateDelivranceIdentite { get; set; }
        public double RevenuMensuelNet { get; set; }
        public double SoldeCompte { get; set; }
        public double SoldeAVA { get; set; }
        public double MouvementAnnuel { get; set; }
        public char CotePersonalisation { get; set; }
        public double PlafondHebdoDAB { get; set; }
        public double PlafondHebdoTPE { get; set; }
    }

    public class VirementRequest
    {
        public string NumCpt { get; set; }
        public decimal Montant { get; set; }
        public string TypeVirement { get; set; }
        public DateTime DateInitiation { get; set; }
        public string CompteBeneficiaire { get; set; }
        public int UtilisateurID { get; set; } // Ajouter cette ligne

        public string  Motif { get; set; }

    }

    public class Virement
    {
        public int Id { get; set; }
        public string NumCpt { get; set; }
        public decimal Montant { get; set; }
        public string TypeVirement { get; set; }
        public bool Validation { get; set; }
        public DateTime DateInitiation { get; set; }
        public DateTime? DateValidation { get; set; }
        public string CompteBeneficiaire { get; set; }
        public string Statut { get; set; } // "Initialisé", "Validé", "Annulé"
    }
    public class VirementValidationRequest
    {
        public int VirementId { get; set; }
    }
    public class Beneficiary
        {
            public string NumCpt { get; set; }
            public decimal Montant { get; set; }
            public string CompteBeneficiaire { get; set; }
            public string NomPrenom { get; set; }
            public string RIB { get; set; }
            public string Motif { get; set; }
            public string Reference { get; set; }
        }
    



}
