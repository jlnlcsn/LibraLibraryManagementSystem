using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LibraLibraryManagementSystem.Models.StudentTransactions;

namespace LibraLibraryManagementSystem.UserPages
{
    public partial class UserTransactions : Page
    {
        private readonly string connectionString;
        private List<StudentTransaction> allReservations;
        private List<StudentTransaction> allHistory;
        private string currentStudentSchoolID;

        public UserTransactions()
        {
            InitializeComponent();
            connectionString = ConfigurationManager.ConnectionStrings["LibraDB"].ConnectionString;

            // Get logged-in student's SchoolID
            currentStudentSchoolID = GetCurrentStudentSchoolID();

            Loaded += UserTransactions_Loaded;

            // Wire up search events
            SearchBoxBorrowed.TextChanged += SearchBoxBorrowed_TextChanged;
            SearchBoxReservations.TextChanged += SearchBoxReservations_TextChanged;

            // Wire up button event
            btnDeclineReservation.Click += BtnCancelReservation_Click;
        }

        private string GetCurrentStudentSchoolID()
        {
            if (Application.Current.Properties.Contains("CurrentStudentSchoolID"))
                return Application.Current.Properties["CurrentStudentSchoolID"]?.ToString();

            return "001"; // fallback
        }

        private void UserTransactions_Loaded(object sender, RoutedEventArgs e)
        {
            LoadReservations();
            LoadHistory();
        }

        /// <summary>
        /// Load all active reservations (Pending, Accepted, Borrowed) for current student
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
                WHERE st.SchoolID = @SchoolID
                  AND st.Status IN ('Pending', 'Accepted', 'Borrowed')
                ORDER BY 
                    CASE 
                        WHEN st.Status = 'Pending' THEN 1
                        WHEN st.Status = 'Accepted' THEN 2
                        WHEN st.Status = 'Borrowed' THEN 3
                        ELSE 4
                    END,
                    st.DateBorrowed DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@SchoolID", currentStudentSchoolID);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Handle SchoolID - can be INT or VARCHAR
                                string schoolID;
                                var schoolIDOrdinal = reader.GetOrdinal("SchoolID");
                                if (reader.IsDBNull(schoolIDOrdinal))
                                {
                                    schoolID = string.Empty;
                                }
                                else
                                {
                                    try
                                    {
                                        schoolID = reader.GetInt32(schoolIDOrdinal).ToString();
                                    }
                                    catch
                                    {
                                        schoolID = reader.GetString(schoolIDOrdinal);
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
                        }
                    }
                }

                dgReservations.ItemsSource = allReservations;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading reservations: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Load transaction history (Declined, Cancelled, Returned) for current student
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
                WHERE st.SchoolID = @SchoolID
                  AND st.Status IN ('Declined', 'Cancelled', 'Returned')
                ORDER BY 
                    CASE 
                        WHEN st.DateReturned IS NOT NULL THEN st.DateReturned
                        ELSE st.DateBorrowed
                    END DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@SchoolID", currentStudentSchoolID);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Handle SchoolID - can be INT or VARCHAR
                                string schoolID;
                                var schoolIDOrdinal = reader.GetOrdinal("SchoolID");
                                if (reader.IsDBNull(schoolIDOrdinal))
                                {
                                    schoolID = string.Empty;
                                }
                                else
                                {
                                    try
                                    {
                                        schoolID = reader.GetInt32(schoolIDOrdinal).ToString();
                                    }
                                    catch
                                    {
                                        schoolID = reader.GetString(schoolIDOrdinal);
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
        /// Cancel a pending reservation
        /// </summary>
        private void BtnCancelReservation_Click(object sender, RoutedEventArgs e)
        {
            if (dgReservations.SelectedItem is StudentTransaction selected)
            {
                // Only allow canceling PENDING reservations
                if (selected.Status?.ToUpper() != "PENDING")
                {
                    MessageBox.Show("Only PENDING reservations can be cancelled.", "Invalid Operation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Cancel reservation for '{selected.Title}'?",
                    "Confirm Cancellation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            conn.Open();

                            // Update status to Cancelled
                            string query = @"
                                UPDATE StudentTransaction
                                SET Status = 'CANCELLED'
                                WHERE TransactionID = @TransactionID";

                            using (SqlCommand cmd = new SqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@TransactionID", selected.TransactionID);
                                int rows = cmd.ExecuteNonQuery();

                                if (rows > 0)
                                {
                                    MessageBox.Show("Reservation cancelled successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                    LoadReservations();
                                    LoadHistory(); // Refresh history to show cancelled transaction
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error cancelling reservation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a reservation to cancel.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Search filter for reservations
        /// </summary>
        private void SearchBoxBorrowed_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (allReservations == null) return;

            string searchText = SearchBoxBorrowed.Text?.ToLower().Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                dgReservations.ItemsSource = allReservations;
                SearchPlaceholderBorrowed.Visibility = Visibility.Visible;
            }
            else
            {
                SearchPlaceholderBorrowed.Visibility = Visibility.Collapsed;
                dgReservations.ItemsSource = allReservations.Where(r =>
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
        }

        /// <summary>
        /// Search filter for history
        /// </summary>
        private void SearchBoxReservations_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (allHistory == null) return;

            string searchText = SearchBoxReservations.Text?.ToLower().Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                dgHistory.ItemsSource = allHistory;
                SearchPlaceholderReservations.Visibility = Visibility.Visible;
            }
            else
            {
                SearchPlaceholderReservations.Visibility = Visibility.Collapsed;
                dgHistory.ItemsSource = allHistory.Where(h =>
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
}