using LibraLibraryManagementSystem.Models;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace LibraLibraryManagementSystem.Controls
{
    public partial class BtnEditBook : Page
    {
        private BookModel _selectedBook;
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["LibraDB"].ConnectionString;

        public BtnEditBook(BookModel selectedBook)
        {
            InitializeComponent();

            _selectedBook = selectedBook ?? throw new ArgumentNullException(nameof(selectedBook));

            LoadBookDetails();

            // Set Status ComboBox options
            cmbStatus.ItemsSource = StatusOptions;
            cmbStatus.SelectedItem = _selectedBook.Status?.ToUpper() ?? "AVAILABLE";

            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
        }

        public string[] StatusOptions => new string[] { "AVAILABLE", "BORROWED", "PENDING SHELVING" };

        private void LoadBookDetails()
        {
            txtBookID.Text = _selectedBook.ID.ToString();
            txtAuthor.Text = _selectedBook.Author ?? string.Empty;
            txtTitle.Text = _selectedBook.Title ?? string.Empty;
            txtPublisher.Text = _selectedBook.Publisher ?? string.Empty;
            txtCategory.Text = _selectedBook.Category ?? string.Empty;
            txtYear.Text = _selectedBook.Year?.ToString() ?? string.Empty;
            txtLocation.Text = _selectedBook.ShelfLocation.ToString();
            txtEdition.Text = _selectedBook.Edition ?? string.Empty;
            txtVolumes.Text = _selectedBook.Volumes?.ToString() ?? string.Empty;
            txtPages.Text = _selectedBook.Pages?.ToString() ?? string.Empty;

            txtEditTitle.Text = "EDIT " + _selectedBook.Title;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAuthor.Text) ||
                string.IsNullOrWhiteSpace(txtTitle.Text) ||
                string.IsNullOrWhiteSpace(txtPublisher.Text) ||
                string.IsNullOrWhiteSpace(txtCategory.Text) ||
                string.IsNullOrWhiteSpace(txtLocation.Text))
            {
                MessageBox.Show("Please fill in all required fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtBookID.Text.Trim(), out int idParsed))
            {
                MessageBox.Show("Book ID must be a number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtLocation.Text.Trim(), out int locParsed))
            {
                MessageBox.Show("Shelf Location must be a number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _selectedBook.ID = idParsed;
            _selectedBook.Author = txtAuthor.Text.Trim();
            _selectedBook.Title = txtTitle.Text.Trim();
            _selectedBook.Publisher = txtPublisher.Text.Trim();
            _selectedBook.Category = txtCategory.Text.Trim();
            _selectedBook.ShelfLocation = locParsed;
            _selectedBook.Edition = txtEdition.Text.Trim();

            if (int.TryParse(txtYear.Text.Trim(), out int yearParsed))
                _selectedBook.Year = yearParsed;

            if (int.TryParse(txtVolumes.Text.Trim(), out int volsParsed))
                _selectedBook.Volumes = volsParsed;

            if (int.TryParse(txtPages.Text.Trim(), out int pagesParsed))
                _selectedBook.Pages = pagesParsed;

            _selectedBook.Status = cmbStatus.SelectedItem?.ToString() ?? "AVAILABLE";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        UPDATE dbo.BOOKS
                        SET 
                            Author=@Author, Title=@Title, Edition=@Edition, Volumes=@Volumes, Pages=@Pages,
                            Publisher=@Publisher, Year=@Year, Category=@Category, ShelfLocation=@ShelfLocation, Status=@Status
                        WHERE ID=@ID";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", _selectedBook.ID);
                        cmd.Parameters.AddWithValue("@Author", _selectedBook.Author);
                        cmd.Parameters.AddWithValue("@Title", _selectedBook.Title);
                        cmd.Parameters.AddWithValue("@Edition", string.IsNullOrWhiteSpace(_selectedBook.Edition) ? (object)DBNull.Value : _selectedBook.Edition);
                        cmd.Parameters.AddWithValue("@Volumes", _selectedBook.Volumes ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Pages", _selectedBook.Pages ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Publisher", _selectedBook.Publisher);
                        cmd.Parameters.AddWithValue("@Year", _selectedBook.Year ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Category", _selectedBook.Category);
                        cmd.Parameters.AddWithValue("@ShelfLocation", _selectedBook.ShelfLocation);
                        cmd.Parameters.AddWithValue("@Status", _selectedBook.Status);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating book:\n" + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show("Book updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            NavigateBack();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => NavigateBack();

        private void NavigateBack()
        {
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
                return;
            }

            if (Application.Current.MainWindow.FindName("fContainer") is Frame f)
                f.Navigate(new LibraLibraryManagementSystem.Pages.Book());
        }

        private void OnLoaded(object sender, RoutedEventArgs e) => UpdateScale();
        private void OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateScale();
        private void UpdateScale()
        {
            double baseWidth = 800, baseHeight = 450;
            double finalScale = Math.Min(ActualWidth / baseWidth, ActualHeight / baseHeight);
            PageScale.ScaleX = finalScale;
            PageScale.ScaleY = finalScale;
        }
    }
}
