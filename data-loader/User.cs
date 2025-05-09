namespace data_loader
{
    public class User
    {
        public string Id { get; set; }
        public Name Name { get; set; }
        public string Email { get; set; }
        public List<string> SecondaryEmails { get; set; }
        public Phone Phone { get; set; }
        public List<Phone> SecondaryPhones { get; set; }
        public DateTime DateOfBirth { get; set; }
        public bool IsActive { get; set; }
    }

    public class Name
    {
        public string First { get; set; }
        public string Last { get; set; }
    }

    public class Phone
    {
        public string Number { get; set; }
        public string Type { get; set; }
    }
}
