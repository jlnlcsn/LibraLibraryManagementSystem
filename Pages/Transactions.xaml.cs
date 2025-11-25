using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Configuration;
using LibraLibraryManagementSystem.Models.StudentTransactions;

namespace LibraLibraryManagementSystem.Pages
{
    public partial class Transactions : Page
    {
        private string connectionString;
        private List<StudentTransaction> allReservations;
        private List<StudentTransaction> allHistory;

        public Transactions()
        {
            InitializeComponent();
            connectionString = ConfigurationManager.ConnectionStrings["LibraDB"].ConnectionString;

            Loaded += Transactions_Loaded;

            // Wire up search events
            SearchBoxBorrowed.TextChanged += SearchBoxBorrowed_TextChanged;
            SearchBoxReservations.TextChanged += SearchBoxReservations_TextChanged;

            // Wire up button events
            btnAddBorrowed.Click += BtnAddBorrowed_Click;
            btnBookReturned.Click += BtnBookReturned_Click;
            btnAcceptReservation.Click += BtnAcceptReservation_Click;
            btnDeclineReservation.Click += BtnDeclineReservation_Click;
        }

        private void Transactions_Loaded(object sender, RoutedEventArgs e)
        {
            LoadReservations();
            LoadHistory();
        }

        /// <summary>
        /// Load all active reservations (Pending, Accepted, Borrowed) from StudentTransaction
        /// </summary>
        private void LoadReservations()
        {
            allReservations = new List<StudentTransaction>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                SELECT 
                    st.TransactionID, 
                    st.BookID, 
                    bm.Title, 
                    st.SchoolID, 
                    su.Name AS BorrowerName,
                    su.GradeSection,
                    su.ContactNo,
                    su.Email,
                    st.DateBorrowed, 
                    st.DueDate, 
                    st.DateReturned, 
                    st.Status
                FROM StudentTransaction st
                LEFT JOIN StudentUser su ON st.SchoolID = su.SchoolID
                LEFT JOIN BookModel bm ON st.BookID = bm.ID
                WHERE UPPER(st.Status) IN ('PENDING', 'ACCEPTED', 'BORROWED')
                ORDER BY 
                    CASE 
                        WHEN UPPER(st.Status) = 'PENDING' THEN 1
                        WHEN UPPER(st.Status) = 'ACCEPTED' THEN 2
                        WHEN UPPER(st.Status) = 'BORROWED' THEN 3
                        ELSE 4
                    END,
                    st.DateBorrowed ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                string schoolID = string.Empty;
                                var schoolIDOrdinal = reader.GetOrdinal("SchoolID");

                                if (!reader.IsDBNull(schoolIDOrdinal))
                                {
                                    var fieldType = reader.GetFieldType(schoolIDOrdinal);

                                    if (fieldType == typeof(int))
                                    {
                                        schoolID = reader.GetInt32(schoolIDOrdinal).ToString();
                                    }
                                    else if (fieldType == typeof(string))
                                    {
                                        schoolID = reader.GetString(schoolIDOrdinal);
                                    }
                                    else
                                    {
                                        schoolID = reader.GetValue(schoolIDOrdinal)?.ToString() ?? string.Empty;
                                    }
                                }

                                allReservations.Add(new StudentTransaction
                                {
                                    TransactionID = reader.GetInt32(reader.GetOrdinal("TransactionID")),
                                    BookID = reader.GetInt32(reader.GetOrdinal("BookID")),
                                    Title = reader.IsDBNull(reader.GetOrdinal("Title")) ? string.Empty : reader.GetString(reader.GetOrdinal("Title")),
                                    SchoolID = schoolID,
                                    BorrowerName = reader.IsDBNull(reader.GetOrdinal("BorrowerName")) ? string.Empty : reader.GetString(reader.GetOrdinal("BorrowerName")),
                                    GradeSection = reader.IsDBNull(reader.GetOrdinal("GradeSection")) ? string.Empty : reader.GetString(reader.GetOrdinal("GradeSection")),
                                    ContactNumber = reader.IsDBNull(reader.GetOrdinal("ContactNo")) ? string.Empty : reader.GetString(reader.GetOrdinal("ContactNo")),
                                    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
                                    DateBorrowed = reader.IsDBNull(reader.GetOrdinal("DateBorrowed")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DateBorrowed")),
                                    DueDate = reader.IsDBNull(reader.GetOrdinal("DueDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DueDate")),
                                    DateReturned = reader.IsDBNull(reader.GetOrdinal("DateReturned")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DateReturned")),
                                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? string.Empty : reader.GetString(reader.GetOrdinal("Status")).ToUpper()
                                });
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error reading reservation row: {ex.Message}\n\nThis row will be skipped.",
                                    "Row Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                    }
                }

                dgReservations.ItemsSource = null;
                dgReservations.ItemsSource = allReservations;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading reservations: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Load all completed transactions (Declined, Cancelled, Returned) from StudentTransaction
        /// </summary>
        private void LoadHistory()
        {
            allHistory = new List<StudentTransaction>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                SELECT 
                    st.TransactionID, 
                    st.BookID, 
                    bm.Title, 
                    st.SchoolID, 
                    su.Name AS BorrowerName,
                    su.GradeSection,
                    su.ContactNo,
                    su.Email,
                    st.DateBorrowed, 
                    st.DueDate, 
                    st.DateReturned, 
                    st.Status
                FROM StudentTransaction st
                LEFT JOIN StudentUser su ON st.SchoolID = su.SchoolID
                LEFT JOIN BookModel bm ON st.BookID = bm.ID
                WHERE UPPER(st.Status) IN ('DECLINED', 'CANCELLED', 'RETURNED')
                ORDER BY 
                    CASE 
                        WHEN st.DateReturned IS NOT NULL THEN st.DateReturned
                        ELSE st.DateBorrowed
                    END DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                string schoolID = string.Empty;
                                var schoolIDOrdinal = reader.GetOrdinal("SchoolID");

                                if (!reader.IsDBNull(schoolIDOrdinal))
                                {
                                    var fieldType = reader.GetFieldType(schoolIDOrdinal);

                                    if (fieldType == typeof(int))
                                    {
                                        schoolID = reader.GetInt32(schoolIDOrdinal).ToString();
                                    }
                                    else if (fieldType == typeof(string))
                                    {
                                        schoolID = reader.GetString(schoolIDOrdinal);
                                    }
                                    else
                                    {
                                        schoolID = reader.GetValue(schoolIDOrdinal)?.ToString() ?? string.Empty;
                                    }
                                }

                                allHistory.Add(new StudentTransaction
                                {
                                    TransactionID = reader.GetInt32(reader.GetOrdinal("TransactionID")),
                                    BookID = reader.GetInt32(reader.GetOrdinal("BookID")),
                                    Title = reader.IsDBNull(reader.GetOrdinal("Title")) ? string.Empty : reader.GetString(reader.GetOrdinal("Title")),
                                    SchoolID = schoolID,
                                    BorrowerName = reader.IsDBNull(reader.GetOrdinal("BorrowerName")) ? string.Empty : reader.GetString(reader.GetOrdinal("BorrowerName")),
                                    GradeSection = reader.IsDBNull(reader.GetOrdinal("GradeSection")) ? string.Empty : reader.GetString(reader.GetOrdinal("GradeSection")),
                                    ContactNumber = reader.IsDBNull(reader.GetOrdinal("ContactNo")) ? string.Empty : reader.GetString(reader.GetOrdinal("ContactNo")),
                                    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
                                    DateBorrowed = reader.IsDBNull(reader.GetOrdinal("DateBorrowed")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DateBorrowed")),
                                    DueDate = reader.IsDBNull(reader.GetOrdinal("DueDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DueDate")),
                                    DateReturned = reader.IsDBNull(reader.GetOrdinal("DateReturned")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DateReturned")),
                                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? string.Empty : reader.GetString(reader.GetOrdinal("Status")).ToUpper()
                                });
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error reading history row: {ex.Message}\n\nThis row will be skipped.",
                                    "Row Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                    }
                }

                dgHistory.ItemsSource = allHistory;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading history: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Accept a pending reservation
        /// </summary>
        private void BtnAcceptReservation_Click(object sender, RoutedEventArgs e)
        {
            if (dgReservations.SelectedItem is StudentTransaction selected)
            {
                if (selected.Status?.ToUpper() != "PENDING")
                {
                    MessageBox.Show("Only PENDING reservations can be accepted.", "Invalid Operation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                UpdateReservationStatus(selected.TransactionID, "ACCEPTED");
                LoadReservations();
            }
            else
            {
                MessageBox.Show("Please select a reservation to accept.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Decline a pending reservation (moves to history)
        /// </summary>
        private void BtnDeclineReservation_Click(object sender, RoutedEventArgs e)
        {
            if (dgReservations.SelectedItem is StudentTransaction selected)
            {
                var result = MessageBox.Show(
                    $"Decline reservation for '{selected.Title}'?",
                    "Confirm Decline",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    UpdateReservationStatus(selected.TransactionID, "DECLINED");
                    LoadReservations();
                    LoadHistory();
                }
            }
            else
            {
                MessageBox.Show("Please select a reservation to decline.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Update reservation status in StudentTransaction table
        /// </summary>
        private void UpdateReservationStatus(int transactionID, string newStatus)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE StudentTransaction SET Status = @Status WHERE TransactionID = @TransactionID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Status", newStatus);
                        cmd.Parameters.AddWithValue("@TransactionID", transactionID);
                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show($"Reservation {newStatus}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating reservation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Mark an accepted reservation as borrowed
        /// </summary>
        private void BtnAddBorrowed_Click(object sender, RoutedEventArgs e)
        {
            if (dgReservations.SelectedItem is StudentTransaction selected)
            {
                if (selected.Status?.ToUpper() != "ACCEPTED")
                {
                    MessageBox.Show("Only ACCEPTED reservations can be marked as borrowed.", "Invalid Operation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        DateTime dateBorrowed = DateTime.Now;
                        DateTime dueDate = dateBorrowed.AddDays(14);

                        string query = @"
                            UPDATE StudentTransaction
                            SET DateBorrowed = @DateBorrowed, 
                                DueDate = @DueDate, 
                                Status = 'BORROWED'
                            WHERE TransactionID = @TransactionID";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@DateBorrowed", dateBorrowed);
                            cmd.Parameters.AddWithValue("@DueDate", dueDate);
                            cmd.Parameters.AddWithValue("@TransactionID", selected.TransactionID);
                            cmd.ExecuteNonQuery();
                        }

                        UpdateBookStatus(conn, selected.BookID, "BORROWED");

                        MessageBox.Show($"Book marked as BORROWED.\nDue Date: {dueDate:MM/dd/yyyy}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadReservations();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating transaction: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select an accepted reservation to mark as borrowed.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Mark a borrowed book as returned (moves to history and sets book to PENDING SHELVING)
        /// </summary>
        private void BtnBookReturned_Click(object sender, RoutedEventArgs e)
        {
            StudentTransaction transaction = dgReservations.SelectedItem as StudentTransaction;

            if (transaction == null)
            {
                MessageBox.Show("Please select a borrowed book to mark as returned.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (transaction.Status?.ToUpper() != "BORROWED")
            {
                MessageBox.Show("Only BORROWED books can be marked as returned.", "Invalid Operation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    DateTime returnedDate = DateTime.Now;

                    // Update transaction status to RETURNED
                    string query = @"
                        UPDATE StudentTransaction
                        SET DateReturned = @DateReturned, 
                            Status = 'RETURNED'
                        WHERE TransactionID = @TransactionID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@DateReturned", returnedDate);
                        cmd.Parameters.AddWithValue("@TransactionID", transaction.TransactionID);
                        cmd.ExecuteNonQuery();
                    }

                    // Update book status to PENDING SHELVING instead of AVAILABLE
                    UpdateBookStatus(conn, transaction.BookID, "PENDING SHELVING");

                    MessageBox.Show("Book returned successfully and moved to history.\nBook status set to PENDING SHELVING.",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadReservations();
                    LoadHistory();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error returning book: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Update book status in BookModel table
        /// </summary>
        private void UpdateBookStatus(SqlConnection conn, int bookID, string status)
        {
            string query = "UPDATE BookModel SET Status = @Status WHERE ID = @BookID";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@BookID", bookID);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Search filter for active reservations
        /// </summary>
        private void SearchBoxBorrowed_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (allReservations == null) return;

            string searchText = SearchBoxBorrowed.Text?.ToLower().Trim() ?? string.Empty;

            dgReservations.ItemsSource = string.IsNullOrWhiteSpace(searchText)
                ? allReservations
                : allReservations.Where(r =>
                    r.BookID.ToString().Contains(searchText) ||
                    (r.Title ?? string.Empty).ToLower().Contains(searchText) ||
                    (r.BorrowerName ?? string.Empty).ToLower().Contains(searchText) ||
                    (r.SchoolID ?? string.Empty).ToLower().Contains(searchText) ||
                    (r.GradeSection ?? string.Empty).ToLower().Contains(searchText) ||
                    (r.ContactNumber ?? string.Empty).ToLower().Contains(searchText) ||
                    (r.Email ?? string.Empty).ToLower().Contains(searchText) ||
                    (r.Status ?? string.Empty).ToLower().Contains(searchText)
                ).ToList();
        }

        /// <summary>
        /// Search filter for transaction history
        /// </summary>
        private void SearchBoxReservations_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (allHistory == null) return;

            string searchText = SearchBoxReservations.Text?.ToLower().Trim() ?? string.Empty;

            dgHistory.ItemsSource = string.IsNullOrWhiteSpace(searchText)
                ? allHistory
                : allHistory.Where(h =>
                    h.BookID.ToString().Contains(searchText) ||
                    (h.Title ?? string.Empty).ToLower().Contains(searchText) ||
                    (h.BorrowerName ?? string.Empty).ToLower().Contains(searchText) ||
                    (h.SchoolID ?? string.Empty).ToLower().Contains(searchText) ||
                    (h.GradeSection ?? string.Empty).ToLower().Contains(searchText) ||
                    (h.ContactNumber ?? string.Empty).ToLower().Contains(searchText) ||
                    (h.Email ?? string.Empty).ToLower().Contains(searchText) ||
                    (h.Status ?? string.Empty).ToLower().Contains(searchText)
                ).ToList();
        }
    }
}