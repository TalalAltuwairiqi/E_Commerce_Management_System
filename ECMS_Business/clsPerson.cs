using System;
using System.Data;
using System.Text.RegularExpressions;
using ECMS_DataAccess;

namespace ECMS_Business
{
    // Business Layer: validation rules and the "save" logic for a person.
    public class clsPerson
    {
        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        private static readonly Regex PhoneRegex = new Regex(@"^\+?[0-9][0-9\s\-]{5,18}[0-9]$");

        public int PersonID { get; private set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool IsNew { get; private set; } = true;

        public string FullName => FirstName + " " + LastName;

        // New (empty) person
        public clsPerson() { }

        // Person loaded from the database
        private clsPerson(DataRow row)
        {
            PersonID = (int)row["PersonID"];
            FirstName = (string)row["FirstName"];
            LastName = (string)row["LastName"];
            Phone = (string)row["Phone"];
            Email = row.IsNull("Email") ? null : (string)row["Email"];
            Address = row.IsNull("Address") ? null : (string)row["Address"];
            DateOfBirth = row.IsNull("DateOfBirth") ? null : (DateTime?)(DateTime)row["DateOfBirth"];
            IsNew = false;
        }

        public static clsPerson? Find(int personID)
        {
            DataTable table = clsPersonData.GetPersonByID(personID);
            return table.Rows.Count == 0 ? null : new clsPerson(table.Rows[0]);
        }

        public static DataTable GetAll(string search)
        {
            return clsPersonData.GetAllPeople(search ?? string.Empty);
        }

        // Checks the rules. Also cleans the values (trims spaces, empty text -> null).
        public bool Validate(out string error)
        {
            FirstName = (FirstName ?? string.Empty).Trim();
            LastName = (LastName ?? string.Empty).Trim();
            Phone = (Phone ?? string.Empty).Trim();
            Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
            Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim();

            if (FirstName.Length == 0) { error = "First name is required."; return false; }
            if (FirstName.Length > 50) { error = "First name must be 50 characters or fewer."; return false; }
            if (LastName.Length == 0) { error = "Last name is required."; return false; }
            if (LastName.Length > 50) { error = "Last name must be 50 characters or fewer."; return false; }
            if (Phone.Length == 0) { error = "Phone is required."; return false; }
            if (!PhoneRegex.IsMatch(Phone)) { error = "Phone must have 7 to 20 digits (spaces, dashes and a leading + are allowed)."; return false; }

            if (Email != null)
            {
                if (Email.Length > 100) { error = "Email must be 100 characters or fewer."; return false; }
                if (!EmailRegex.IsMatch(Email)) { error = "Email format is not valid."; return false; }
            }

            if (Address != null && Address.Length > 250) { error = "Address must be 250 characters or fewer."; return false; }

            if (DateOfBirth.HasValue &&
                (DateOfBirth.Value.Date > DateTime.Today || DateOfBirth.Value.Year < 1900))
            { error = "Date of birth is not valid."; return false; }

            error = string.Empty;
            return true;
        }

        // Adds the person if new, otherwise updates it.
        public bool Save(out string error)
        {
            if (!Validate(out error))
                return false;

            if (Email != null && clsPersonData.IsEmailUsed(Email, PersonID))
            {
                error = "This email is already used by another person.";
                return false;
            }

            if (IsNew)
            {
                int newID = clsPersonData.AddNewPerson(FirstName, LastName, Phone, Email, Address, DateOfBirth);
                if (newID <= 0)
                {
                    error = "The person could not be saved.";
                    return false;
                }

                PersonID = newID;
                IsNew = false;
                return true;
            }

            if (!clsPersonData.UpdatePerson(PersonID, FirstName, LastName, Phone, Email, Address, DateOfBirth))
            {
                error = "The person could not be updated.";
                return false;
            }

            return true;
        }

        // Only an Admin can delete, and only people who are not customers or users.
        public static bool Delete(int personID, clsUser? actingUser, out string error)
        {
            if (actingUser == null || !actingUser.IsAdmin)
            {
                error = "Only an administrator can delete people.";
                return false;
            }

            if (clsPersonData.IsPersonLinked(personID))
            {
                error = "This person is linked to a customer or a staff user and cannot be deleted.";
                return false;
            }

            if (!clsPersonData.DeletePerson(personID))
            {
                error = "The person could not be deleted.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}