using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LibraLibraryManagementSystem.Pages
{
    public partial class Dashboard : Page
    {
        private string connectionString;

        public Dashboard()
        {
            InitializeComponent();
            connectionString = ConfigurationManager.ConnectionStrings["LibraDB"].ConnectionString;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadDashboardData();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Optional: Add responsive scaling logic here if needed
        }

        /// <summary>
        /// Load all dashboard statistics and data
        /// </summary>
        private void LoadDashboardData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Load statistics
                    LoadTotalBooks(conn);
                    LoadCurrentBooks(conn);
                    LoadBorrowedBooks(conn);
                    LoadBooksToShelve(conn);

                    // Load due today books
                    LoadDueTodayBooks(conn);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard data: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Get total number of books in the library
        /// </summary>
        private void LoadTotalBooks(SqlConnection conn)
        {
            try
            {
                string query = "SELECT COUNT(*) FROM BookModel";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    int count = (int)cmd.ExecuteScalar();
                    TotalBooksLabel.Text = count.ToString();
                }
            }
            catch (Exception ex)
            {
                TotalBooksLabel.Text = "Error";
                System.Diagnostics.Debug.WriteLine($"Error loading total books: {ex.Message}");
            }
        }

        /// <summary>
        /// Get current number of available books (AVAILABLE status)
        /// </summary>
        private void LoadCurrentBooks(SqlConnection conn)
        {
            try
            {
                string query = "SELECT COUNT(*) FROM BookModel WHERE UPPER(Status) = 'AVAILABLE'";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    int count = (int)cmd.ExecuteScalar();
                    CurrentBooksLabel.Text = count.ToString();
                }
            }
            catch (Exception ex)
            {
                CurrentBooksLabel.Text = "Error";
                System.Diagnostics.Debug.WriteLine($"Error loading current books: {ex.Message}");
            }
        }

        /// <summary>
        /// Get number of currently borrowed books (BORROWED status)
        /// </summary>
        private void LoadBorrowedBooks(SqlConnection conn)
        {
            try
            {
                string query = "SELECT COUNT(*) FROM BookModel WHERE UPPER(Status) = 'BORROWED'";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    int count = (int)cmd.ExecuteScalar();
                    BorrowedBooksLabel.Text = count.ToString();
                }
            }
            catch (Exception ex)
            {
                BorrowedBooksLabel.Text = "Error";
                System.Diagnostics.Debug.WriteLine($"Error loading borrowed books: {ex.Message}");
            }
        }

        /// <summary>
        /// Get number of books pending shelving (PENDING SHELVING status)
        /// </summary>
        private void LoadBooksToShelve(SqlConnection conn)
        {
            try
            {
                string query = "SELECT COUNT(*) FROM BookModel WHERE UPPER(Status) = 'PENDING SHELVING'";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    int count = (int)cmd.ExecuteScalar();
                    BooksToShelveLabel.Text = count.ToString();
                }
            }
            catch (Exception ex)
            {
                BooksToShelveLabel.Text = "Error";
                System.Diagnostics.Debug.WriteLine($"Error loading books to shelve: {ex.Message}");
            }
        }

        /// <summary>
        /// Load books that are due today
        /// </summary>
        private void LoadDueTodayBooks(SqlConnection conn)
        {
            try
            {
                var dueTodayList = new List<DueTodayItem>();

                string query = @"
                    SELECT 
                        st.BookID,
                        bm.Title AS BookTitle,
                        su.Name AS Borrower,
                        su.ContactNo AS Contact,
                        st.DueDate
                    FROM StudentTransaction st
                    INNER JOIN BookModel bm ON st.BookID = bm.ID
                    INNER JOIN StudentUser su ON st.SchoolID = su.SchoolID
                    WHERE UPPER(st.Status) = 'BORROWED'
                      AND CONVERT(DATE, st.DueDate) = CONVERT(DATE, GETDATE())
                    ORDER BY st.DueDate ASC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        dueTodayList.Add(new DueTodayItem
                        {
                            BookID = reader.GetInt32(reader.GetOrdinal("BookID")),
                            BookTitle = reader.IsDBNull(reader.GetOrdinal("BookTitle"))
                                ? "Unknown"
                                : reader.GetString(reader.GetOrdinal("BookTitle")),
                            Borrower = reader.IsDBNull(reader.GetOrdinal("Borrower"))
                                ? "Unknown"
                                : reader.GetString(reader.GetOrdinal("Borrower")),
                            Contact = reader.IsDBNull(reader.GetOrdinal("Contact"))
                                ? "N/A"
                                : reader.GetString(reader.GetOrdinal("Contact"))
                        });
                    }
                }

                DueTodayDataGrid.ItemsSource = dueTodayList;

                // Update the title with count
                DueTodayTitle.Text = $"DUE TODAY ({dueTodayList.Count})";
            }
            catch (Exception ex)
            {
                DueTodayTitle.Text = "DUE TODAY (Error)";
                System.Diagnostics.Debug.WriteLine($"Error loading due today books: {ex.Message}");
            }
        }

        // Event handlers for Loaded events (alternative to calling in OnLoaded)
        private void TotalBooksLabel_Loaded(object sender, RoutedEventArgs e)
        {
            // Initial value set by LoadDashboardData
        }

        private void CurrentBooksLabel_Loaded(object sender, RoutedEventArgs e)
        {
            // Initial value set by LoadDashboardData
        }

        private void BorrowedBooksLabel_Loaded(object sender, RoutedEventArgs e)
        {
            // Initial value set by LoadDashboardData
        }

        private void BooksToShelveLabel_Loaded(object sender, RoutedEventArgs e)
        {
            // Initial value set by LoadDashboardData
        }

        private void DueTodayTitle_Loaded(object sender, RoutedEventArgs e)
        {
            // Initial value set by LoadDashboardData
        }
    }

    /// <summary>
    /// Model for Due Today items in the DataGrid
    /// </summary>
    public class DueTodayItem
    {
        public int BookID { get; set; }
        public string BookTitle { get; set; }
        public string Borrower { get; set; }
        public string Contact { get; set; }
    }
}