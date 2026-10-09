using System;
using System.Data;
using ECMS_DataAccess;

namespace ECMS_Business
{
    public class clsCustomer
    {
        public int CustomerID { get; private set; }
        public int PersonID { get; set; }
        public DateTime CreatedAt { get; private set; }
        public bool IsActive { get; set; } = true;
        public bool IsNew { get; private set; } = true;

        // New customer
        public clsCustomer() { }

        // Customer loaded from the database
        private clsCustomer(DataRow row)
        {
            CustomerID = (int)row["CustomerID"];
            PersonID = (int)row["PersonID"];
            CreatedAt = (DateTime)row["CreatedAt"];
            IsActive = (bool)row["IsActive"];
            IsNew = false;
        }

        // The personal data of this customer (name, phone, ...)
        public clsPerson? GetPerson()
        {
            return clsPerson.Find(PersonID);
        }

        public static clsCustomer? Find(int customerID)
        {
            DataTable table = clsCustomerData.GetCustomerByID(customerID);
            return table.Rows.Count == 0 ? null : new clsCustomer(table.Rows[0]);
        }

        public static DataTable GetAll(string search, bool activeOnly)
        {
            return clsCustomerData.GetAllCustomers(search ?? string.Empty, activeOnly);
        }

        public static DataTable GetPeopleWithoutCustomer()
        {
            return clsCustomerData.GetPeopleWithoutCustomer();
        }

        public static DataTable GetOrders(int customerID)
        {
            return clsCustomerData.GetCustomerOrders(customerID);
        }

        public bool Save(out string error)
        {
            if (IsNew)
            {
                if (PersonID <= 0)
                {
                    error = "Please select a person (or create a new one).";
                    return false;
                }

                if (clsCustomerData.IsPersonAlreadyCustomer(PersonID))
                {
                    error = "This person is already a customer.";
                    return false;
                }

                int newID = clsCustomerData.AddNewCustomer(PersonID, IsActive);
                if (newID <= 0)
                {
                    error = "The customer could not be saved.";
                    return false;
                }

                CustomerID = newID;
                IsNew = false;
                error = string.Empty;
                return true;
            }

            if (!clsCustomerData.UpdateCustomer(CustomerID, IsActive))
            {
                error = "The customer could not be updated.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}