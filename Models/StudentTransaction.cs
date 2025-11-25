using System;

namespace LibraLibraryManagementSystem.Models.StudentTransactions
{
    public class StudentTransaction
    {
        public int TransactionID { get; set; }       // Auto-incremented ID
        public int BookID { get; set; }              // FK to Books table
        public string Title { get; set; }            // Book title at time of request
        public string SchoolID { get; set; }         // FK to StudentUser
        public string BorrowerName { get; set; }
        public DateTime? DateBorrowed { get; set; }  // Null until book is borrowed
        public DateTime? DueDate { get; set; }       // Null until book is borrowed
        public DateTime? DateReturned { get; set; }  // Null until book is returned
        public string Status { get; set; }           // Pending, Accepted, Declined, Cancelled, Borrowed
        public string GradeSection { get; internal set; }
        public string ContactNumber { get; internal set; }
        public string Email { get; internal set; }
    }
}
