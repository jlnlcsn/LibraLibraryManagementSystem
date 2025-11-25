using LibraLibraryManagementSystem.Models;
using Book = LibraLibraryManagementSystem.Models.BookModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LibraLibraryManagementSystem.UserPages
{
    public partial class UserBooks : Page
    {
        private readonly string connectionString;
        private List<Book> allBooks;
        private List<Book> filteredBooks;
        private string currentStudentSchoolID;
        private string currentStudentName;
        
        // ------------------------------
        // ADDED (to match Admin version)
        // ------------------------------
        private int earliestYear = 1970;
        private int latestYear = DateTime.Now.Year;
        // ------------------------------

        public UserBooks()
        {
            InitializeComponent();
            connectionString = ConfigurationManager.ConnectionStrings["LibraDB"].ConnectionString;

            currentStudentSchoolID = GetCurrentStudentSchoolID();
            currentStudentName = GetCurrentStudentName();

            Loaded += UserBooks_Loaded;

            UserSearchBox.TextChanged += OnFilterTextChanged;
            UserYearSlider.ValueChanged += OnYearSliderChanged;
        }

        private string GetCurrentStudentSchoolID()
        {
            if (Application.Current.Properties.Contains("CurrentStudentSchoolID"))
            {
                string schoolID = Application.Current.Properties["CurrentStudentSchoolID"]?.ToString();

                if (!string.IsNullOrEmpty(schoolID))
                {
                    return schoolID;
                }
            }

            // If we reach here, something is wrong with the login
            MessageBox.Show(
                "Student information not found. Please log in again.",
                "Session Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            // You might want to redirect to login page here
            // NavigationService?.Navigate(new Uri("LoginPage.xaml", UriKind.Relative));

            return null; // Return null instead of fallback so we know there's an issue
        }

        private string GetCurrentStudentName()
        {
            if (Application.Current.Properties.Contains("CurrentStudentName"))
            {
                string name = Application.Current.Properties["CurrentStudentName"]?.ToString();

                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }

            MessageBox.Show(
                "Student information not found. Please log in again.",
                "Session Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return null;
        }

        private void UserBooks_Loaded(object sender, RoutedEventArgs e)
        {
            LoadBooks();
            InitializeFilters();
        }

        private void LoadBooks()
        {
            allBooks = new List<Book>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                SELECT ID, Author, Title, Publisher, Year, Edition, Volumes, Pages, Category, ShelfLocation, Status
                FROM BookModel
                ORDER BY Title";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var book = new Book
                            {
                                ID = reader.GetInt32(0),
                                Author = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                Title = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                Publisher = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                Year = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                                Edition = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                Volumes = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                                Pages = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7),
                                Category = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                                ShelfLocation = reader.IsDBNull(9) ? 0 :
                                    (reader.GetFieldType(9) == typeof(int) ? reader.GetInt32(9) :
                                     int.TryParse(reader.GetString(9), out var sl) ? sl : 0),
                                Status = reader.IsDBNull(10) ? "AVAILABLE" : reader.GetString(10).ToUpper()
                            };

                            // Remove or comment out ParseStatus() if it's causing issues
                            // book.ParseStatus();

                            allBooks.Add(book);
                        }
                    }
                }

                filteredBooks = new List<Book>(allBooks);
                UserBooksDataGrid.ItemsSource = filteredBooks;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading books: {ex.Message}",
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // ------------------------------------------------------
        // UPDATED InitializeFilters() to match Admin version
        // ------------------------------------------------------
        private void InitializeFilters()
        {
            if (allBooks.Any(b => b.Year.HasValue))
            {
                earliestYear = allBooks.Where(b => b.Year.HasValue).Min(b => b.Year.Value);
                latestYear = allBooks.Where(b => b.Year.HasValue).Max(b => b.Year.Value);
            }
            else
            {
                earliestYear = 1970;
                latestYear = DateTime.Now.Year;
            }

            UserYearSlider.Minimum = 0;
            UserYearSlider.Maximum = latestYear - earliestYear + 1;
            UserYearSlider.Value = 0;

            UpdateYearLabel();
        }

        // ------------------------------------------------------
        // SAME AS ADMIN Books version
        // ------------------------------------------------------
        private int SliderValueToYear(int sliderValue)
        {
            return sliderValue == 0 ? 0 : earliestYear + (sliderValue - 1);
        }

        private void OnFilterTextChanged(object sender, TextChangedEventArgs e)
            => ApplyFilters();

        // ------------------------------------------------------
        // UPDATED to match Admin behavior
        // ------------------------------------------------------
        private void OnYearSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateYearLabel();
            ApplyFilters();
        }

        private void UpdateYearLabel()
        {
            int year = SliderValueToYear((int)UserYearSlider.Value);
            UserYearValue.Text = year == 0 ? "" : year.ToString();
        }

        private void OnFilterCheckedChanged(object sender, RoutedEventArgs e)
            => ApplyFilters();

        // ------------------------------------------------------
        // ApplyFilters WITH FIXED YEAR FILTER (matches Admin)
        // ------------------------------------------------------
        private void ApplyFilters()
        {
            if (allBooks == null) return;

            filteredBooks = new List<Book>(allBooks);

            // SEARCH
            string searchText = (UserSearchBox.Text ?? "").Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filteredBooks = filteredBooks.Where(b =>
                    (b.Title ?? "").ToLower().Contains(searchText) ||
                    (b.Author ?? "").ToLower().Contains(searchText) ||
                    (b.Category ?? "").ToLower().Contains(searchText) ||
                    (b.Publisher ?? "").ToLower().Contains(searchText) ||
                    b.ID.ToString().Contains(searchText)
                ).ToList();
            }

            // ---------------------------
            // FIXED YEAR FILTER
            // ---------------------------
            int sliderYear = SliderValueToYear((int)UserYearSlider.Value);
            if (sliderYear != 0)
            {
                filteredBooks = filteredBooks
                    .Where(b => b.Year.HasValue && b.Year.Value == sliderYear)
                    .ToList();
            }

            // CATEGORY FILTER
            var selectedCategories = UserCategoryPanel.Children.OfType<CheckBox>()
                .Where(cb => cb.IsChecked == true)
                .Select(cb => cb.Content.ToString())
                .ToList();

            if (selectedCategories.Any())
            {
                filteredBooks = filteredBooks
                    .Where(b => selectedCategories.Contains(b.Category))
                    .ToList();
            }

            // STATUS FILTER
            var selectedStatuses = UserStatusPanel.Children.OfType<CheckBox>()
                .Where(cb => cb.IsChecked == true)
                .Select(cb => cb.Content.ToString().ToUpper())
                .ToList();

            if (selectedStatuses.Any())
            {
                filteredBooks = filteredBooks
                    .Where(b => selectedStatuses.Contains((b.Status ?? "").ToUpper()))
                    .ToList();
            }

            UserBooksDataGrid.ItemsSource = filteredBooks;
        }

        // -------------------------
        // RESERVATION LOGIC (same)
        // -------------------------
        private void AddReserve_Click(object sender, RoutedEventArgs e)
        {
            if (UserBooksDataGrid.SelectedItem is Book selectedBook)
            {
                if ((selectedBook.Status ?? "").ToUpper() != "AVAILABLE")
                {
                    MessageBox.Show(
                        $"This book is currently {selectedBook.Status}. Only AVAILABLE books can be reserved.",
                        "Book Unavailable",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (HasExistingReservation(selectedBook.ID))
                {
                    MessageBox.Show(
                        "You already have an active reservation for this book.",
                        "Duplicate Reservation",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                int activeReservations = GetActiveReservationCount();
                const int MAX_RESERVATIONS = 5;

                if (activeReservations >= MAX_RESERVATIONS)
                {
                    MessageBox.Show(
                        $"You reached the max limit of {MAX_RESERVATIONS} active reservations.",
                        "Limit Reached",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Reserve this book?\n\nTitle: {selectedBook.Title}",
                    "Confirm Reservation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            conn.Open();

                            string query = @"
                        INSERT INTO StudentTransaction
                        (BookID, SchoolID, Status, DateBorrowed)
                        VALUES
                        (@BookID, @SchoolID, 'Pending', @DateBorrowed)";

                            using (SqlCommand cmd = new SqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@BookID", selectedBook.ID);

                                // Parse SchoolID as INT
                                if (int.TryParse(currentStudentSchoolID, out int schoolIdInt))
                                {
                                    cmd.Parameters.AddWithValue("@SchoolID", schoolIdInt);
                                }
                                else
                                {
                                    MessageBox.Show("Invalid student ID. Please log in again.",
                                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                cmd.Parameters.AddWithValue("@DateBorrowed", DateTime.Now);

                                cmd.ExecuteNonQuery();

                                MessageBox.Show(
                                    "Reservation submitted successfully!",
                                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}",
                            "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Select a book first.",
                    "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool HasExistingReservation(int bookID)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                SELECT COUNT(*) 
                FROM StudentTransaction
                WHERE BookID = @BookID 
                  AND SchoolID = @SchoolID
                  AND Status IN ('PENDING', 'ACCEPTED', 'BORROWED')";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@BookID", bookID);

                        if (int.TryParse(currentStudentSchoolID, out int schoolIdInt))
                        {
                            cmd.Parameters.AddWithValue("@SchoolID", schoolIdInt);
                        }
                        else
                        {
                            return false;
                        }

                        return (int)cmd.ExecuteScalar() > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private int GetActiveReservationCount()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                SELECT COUNT(*) 
                FROM StudentTransaction
                WHERE SchoolID = @SchoolID
                  AND Status IN ('PENDING', 'ACCEPTED', 'BORROWED')";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        if (int.TryParse(currentStudentSchoolID, out int schoolIdInt))
                        {
                            cmd.Parameters.AddWithValue("@SchoolID", schoolIdInt);
                        }
                        else
                        {
                            return 0;
                        }

                        return (int)cmd.ExecuteScalar();
                    }
                }
            }
            catch
            {
                return 0;
            }
        }
        
    }
}
