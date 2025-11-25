using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using LibraLibraryManagementSystem.Models;
using LibraLibraryManagementSystem.Pages;

namespace LibraLibraryManagementSystem.Controls
{
    public partial class BtnAddBook_Page : Page
    {
        private readonly string connectionString;

        public BtnAddBook_Page()
        {
            InitializeComponent();

            connectionString = ConfigurationManager.ConnectionStrings["LibraDB"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                MessageBox.Show("Connection string 'LibraDB' not found.",
                                "Configuration Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }

            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // --- REQUIRED FIELD VALIDATION (remove txtBookID) ---
            if (string.IsNullOrWhiteSpace(txtAuthor.Text) ||
                string.IsNullOrWhiteSpace(txtTitle.Text) ||
                string.IsNullOrWhiteSpace(txtPublisher.Text) ||
                string.IsNullOrWhiteSpace(txtCategory.Text) ||
                string.IsNullOrWhiteSpace(txtLocation.Text))
            {
                MessageBox.Show("Please fill in all required fields.",
                                "Validation Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            var model = new BookModel
            {
                Author = txtAuthor.Text.Trim(),
                Title = txtTitle.Text.Trim(),
                Publisher = txtPublisher.Text.Trim(),
                Category = txtCategory.Text.Trim(),
                Status = "Available" // default
            };

            // Year
            if (int.TryParse(txtYear.Text.Trim(), out int yearParsed))
                model.Year = yearParsed;
            else
                model.Year = null;

            // ShelfLocation
            if (!int.TryParse(txtLocation.Text.Trim(), out int locParsed))
            {
                MessageBox.Show("Shelf Location must be a number.",
                                "Validation Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }
            model.ShelfLocation = locParsed;

            // Optional fields
            model.Edition = txtEdition.Text.Trim();
            if (int.TryParse(txtVolumes.Text.Trim(), out int volsParsed))
                model.Volumes = volsParsed;
            else
                model.Volumes = null;  // Now properly null

            // Pages
            if (int.TryParse(txtPages.Text.Trim(), out int pagesParsed))
                model.Pages = pagesParsed;
            else
                model.Pages = null;  // Now properly null

            // --- DATABASE INSERT (without ID) ---
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        INSERT INTO dbo.BookModel
                        (Author, Title, Edition, Volumes, Pages, Publisher, Year, Category, ShelfLocation, Status)
                        VALUES 
                        (@Author, @Title, @Edition, @Volumes, @Pages, @Publisher, @Year, @Category, @ShelfLocation, @Status);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Author", model.Author);
                        cmd.Parameters.AddWithValue("@Title", model.Title);
                        cmd.Parameters.AddWithValue("@Edition",
                            string.IsNullOrWhiteSpace(model.Edition) ? (object)DBNull.Value : model.Edition);
                        cmd.Parameters.AddWithValue("@Volumes",
                            model.Volumes.HasValue ? (object)model.Volumes.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Pages",
                            model.Pages.HasValue ? (object)model.Pages.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Publisher", model.Publisher);
                        cmd.Parameters.AddWithValue("@Year",
                            model.Year.HasValue ? (object)model.Year.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Category",
                            string.IsNullOrWhiteSpace(model.Category) ? (object)DBNull.Value : model.Category);
                        cmd.Parameters.AddWithValue("@ShelfLocation", model.ShelfLocation);
                        cmd.Parameters.AddWithValue("@Status", model.Status);

                        // Get generated ID
                        model.ID = (int)cmd.ExecuteScalar();
                    }

                }

                // Display ID as read-only
                txtBookID.Text = model.ID.ToString();
                txtBookID.IsReadOnly = true;

                // Notify Book page
                Book.RaiseBookAdded(model);

                MessageBox.Show("Book successfully added.",
                                "Success",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                NavigateBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding book:\n" + ex.Message,
                                "Database Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateBack();
        }

        private void NavigateBack()
        {
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
                return;
            }

            var main = Application.Current.MainWindow;
            if (main != null)
            {
                var f = main.FindName("fContainer") as Frame;
                if (f != null)
                {
                    f.Navigate(new LibraLibraryManagementSystem.Pages.Book());
                }
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateScale();

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT ISNULL(IDENT_CURRENT('BookModel'), 0)";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        decimal nextId = (decimal)cmd.ExecuteScalar();
                        txtBookID.Text = nextId.ToString();
                        txtBookID.IsReadOnly = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error fetching next Book ID:\n" + ex.Message,
                                "Database Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateScale();
        }

        private void UpdateScale()
        {
            double baseWidth = 800;
            double baseHeight = 450;

            double scaleX = ActualWidth / baseWidth;
            double scaleY = ActualHeight / baseHeight;

            double finalScale = (scaleX < scaleY) ? scaleX : scaleY;

            PageScale.ScaleX = finalScale;
            PageScale.ScaleY = finalScale;
        }
    }
}
