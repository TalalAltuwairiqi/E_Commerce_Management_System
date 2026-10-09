using System;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using ECMS_DataAccess;

namespace ECMS_Business
{
    public enum enLoginResult
    {
        Success,
        WrongCredentials,   // used for both "no such user" and "wrong password" (do not reveal which)
        Inactive
    }

    // Business Layer: login, user management and password rules live here, not in the forms.
    public class clsUser
    {
        private const int BCryptWorkFactor = 11;
        private static readonly Regex UsernameRegex = new Regex(@"^[A-Za-z0-9._]{3,50}$");

        public int UserID { get; private set; }
        public int PersonID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; private set; } = string.Empty;
        public string Role { get; set; } = "Staff";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; private set; }
        public bool IsNew { get; private set; } = true;

        public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);

        // New (empty) user
        public clsUser() { }

        // Used by Login
        private clsUser(int userID, int personID, string username, string fullName, string role, bool isActive)
        {
            UserID = userID;
            PersonID = personID;
            Username = username;
            FullName = fullName;
            Role = role;
            IsActive = isActive;
            IsNew = false;
        }

        // User loaded from the database
        private clsUser(DataRow row)
        {
            UserID = (int)row["UserID"];
            PersonID = (int)row["PersonID"];
            Username = (string)row["Username"];
            FullName = (string)row["FullName"];
            Role = (string)row["Role"];
            IsActive = (bool)row["IsActive"];
            CreatedAt = (DateTime)row["CreatedAt"];
            IsNew = false;
        }

        // ------------------------------------------------------------------
        // Login
        // ------------------------------------------------------------------
        public static enLoginResult Login(string username, string password, out clsUser? user)
        {
            user = null;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                return enLoginResult.WrongCredentials;

            username = username.Trim();

            if (!clsUserData.GetUserByUsername(username,
                    out int userID, out int personID, out string passwordHash,
                    out string role, out bool isActive, out string fullName))
                return enLoginResult.WrongCredentials;

            // BCrypt compares the typed password with the stored hash.
            if (!BCrypt.Net.BCrypt.Verify(password, passwordHash))
                return enLoginResult.WrongCredentials;

            if (!isActive)
                return enLoginResult.Inactive;

            user = new clsUser(userID, personID, username, fullName, role, isActive);
            return enLoginResult.Success;
        }

        // ------------------------------------------------------------------
        // Finding users
        // ------------------------------------------------------------------
        public static clsUser? Find(int userID)
        {
            DataTable table = clsUserData.GetUserByID(userID);
            return table.Rows.Count == 0 ? null : new clsUser(table.Rows[0]);
        }

        public static DataTable GetAll(string search, bool activeOnly)
        {
            return clsUserData.GetAllUsers(search ?? string.Empty, activeOnly);
        }

        public static DataTable GetPeopleWithoutUser()
        {
            return clsUserData.GetPeopleWithoutUser();
        }

        public clsPerson? GetPerson()
        {
            return clsPerson.Find(PersonID);
        }

        // ------------------------------------------------------------------
        // Password rules
        // ------------------------------------------------------------------
        public static bool ValidatePassword(string password, out string error)
        {
            if (password == null || password.Length < 8)
            {
                error = "Password must be at least 8 characters.";
                return false;
            }

            if (password.Length > 64)
            {
                error = "Password must be 64 characters or fewer.";
                return false;
            }

            if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            {
                error = "Password must contain at least one letter and one digit.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        // ------------------------------------------------------------------
        // Add / update a user (Admin only)
        // "password" is only used when the user is new.
        // ------------------------------------------------------------------
        public bool Save(string? password, clsUser? actingUser, out string error)
        {
            if (actingUser == null || !actingUser.IsAdmin)
            {
                error = "Only an administrator can manage users.";
                return false;
            }

            Username = (Username ?? string.Empty).Trim();

            if (!UsernameRegex.IsMatch(Username))
            {
                error = "Username must be 3 to 50 characters: letters, digits, dot or underscore.";
                return false;
            }

            if (!string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(Role, "Staff", StringComparison.OrdinalIgnoreCase))
            {
                error = "Role must be Admin or Staff.";
                return false;
            }
            Role = IsAdmin ? "Admin" : "Staff";   // normalise the spelling

            if (clsUserData.IsUsernameUsed(Username, IsNew ? 0 : UserID))
            {
                error = "This username is already taken.";
                return false;
            }

            if (IsNew)
            {
                if (PersonID <= 0)
                {
                    error = "Please select a person (or create a new one).";
                    return false;
                }

                if (clsUserData.IsPersonAlreadyUser(PersonID))
                {
                    error = "This person already has a user account.";
                    return false;
                }

                string newPassword = password ?? string.Empty;
                if (!ValidatePassword(newPassword, out error))
                    return false;

                string hash = BCrypt.Net.BCrypt.HashPassword(newPassword, BCryptWorkFactor);
                int newID = clsUserData.AddNewUser(PersonID, Username, hash, Role, IsActive);
                if (newID <= 0)
                {
                    error = "The user could not be saved.";
                    return false;
                }

                UserID = newID;
                IsNew = false;
                error = string.Empty;
                return true;
            }

            // ----- Update protections -----
            // 1) You cannot lock yourself out.
            if (UserID == actingUser.UserID && (!IsActive || !IsAdmin))
            {
                error = "You cannot deactivate your own account or remove your own Admin role.";
                return false;
            }

            // 2) At least one active Admin must always remain.
            bool remainsActiveAdmin = IsActive && IsAdmin;
            if (!remainsActiveAdmin && clsUserData.CountActiveAdminsExcluding(UserID) == 0)
            {
                error = "At least one active Admin must remain.";
                return false;
            }

            if (!clsUserData.UpdateUser(UserID, Username, Role, IsActive))
            {
                error = "The user could not be updated.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        // ------------------------------------------------------------------
        // Change the password of the logged-in user (needs the current password)
        // ------------------------------------------------------------------
        public static bool ChangeOwnPassword(clsUser? actingUser, string currentPassword, string newPassword, out string error)
        {
            if (actingUser == null)
            {
                error = "You must be logged in.";
                return false;
            }

            string? storedHash = clsUserData.GetPasswordHash(actingUser.UserID);
            if (storedHash == null || !BCrypt.Net.BCrypt.Verify(currentPassword ?? string.Empty, storedHash))
            {
                error = "The current password is not correct.";
                return false;
            }

            if (currentPassword == newPassword)
            {
                error = "The new password must be different from the current one.";
                return false;
            }

            if (!ValidatePassword(newPassword, out error))
                return false;

            string newHash = BCrypt.Net.BCrypt.HashPassword(newPassword, BCryptWorkFactor);
            if (!clsUserData.UpdatePassword(actingUser.UserID, newHash))
            {
                error = "The password could not be changed.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        // ------------------------------------------------------------------
        // Admin sets a new password for another user (no current password needed)
        // ------------------------------------------------------------------
        public static bool ResetPassword(clsUser? actingUser, int userID, string newPassword, out string error)
        {
            if (actingUser == null || !actingUser.IsAdmin)
            {
                error = "Only an administrator can reset passwords.";
                return false;
            }

            if (!ValidatePassword(newPassword, out error))
                return false;

            string newHash = BCrypt.Net.BCrypt.HashPassword(newPassword, BCryptWorkFactor);
            if (!clsUserData.UpdatePassword(userID, newHash))
            {
                error = "The password could not be reset.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}