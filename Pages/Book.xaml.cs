using LibraLibraryManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LibraLibraryManagementSystem.Pages
{
    public partial class Book : Page
    {
        public static event Action<BookModel> BookAdded;
        public static void RaiseBookAdded(BookModel model) => BookAdded?.Invoke(model);

        private ObservableCollection<BookModel> Books { get; } = new ObservableCollection<BookModel>();
        private ICollectionView _view;
        private readonly string connectionString;

        private int earliestYear = 1970;
        private int latestYear = DateTime.Now.Year;

        public Book()
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            connectionString = ConfigurationManager.ConnectionStrings["LibraDB"]?.ConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                MessageBox.Show("Connection string 'LibraDB' not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadBooksFromDatabase();

            _view = CollectionViewSource.GetDefaultView(Books);
            _view.Filter = FilterBooks;

            BooksDataGrid.ItemsSource = _view;

            BookAdded += OnExternalBookAdded;
            Loaded += Book_Loaded;
        }

        private void Book_Loaded(object sender, RoutedEventArgs e) => RefreshBooks();

        private void OnExternalBookAdded(BookModel model)
        {
            Dispatcher.Invoke(() =>
            {
                Books.Add(model);
                _view?.Refresh();
                UpdateYearSlider();
            });
        }

        public void RefreshBooks()
        {
            LoadBooksFromDatabase();
            _view?.Refresh();
            UpdateYearSlider();
        }

        private void LoadBooksFromDatabase()
        {
            try
            {
                Books.Clear();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = "SELECT * FROM dbo.BookModel";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var model = new BookModel
                            {
                                ID = reader["ID"] != DBNull.Value ? Convert.ToInt32(reader["ID"]) : 0,
                                Author = reader["Author"]?.ToString(),
                                Title = reader["Title"]?.ToString(),
                                Edition = reader["Edition"]?.ToString(),
                                Volumes = reader["Volumes"] != DBNull.Value ? Convert.ToInt32(reader["Volumes"]) : (int?)null,
                                Pages = reader["Pages"] != DBNull.Value ? Convert.ToInt32(reader["Pages"]) : (int?)null,
                                Year = reader["Year"] != DBNull.Value ? Convert.ToInt32(reader["Year"]) : (int?)null,
                                Category = reader["Category"]?.ToString(),
                                ShelfLocation = reader["ShelfLocation"] != DBNull.Value ? Convert.ToInt32(reader["ShelfLocation"]) : 0,
                                Status = reader["Status"]?.ToString()?.ToUpper() ?? "AVAILABLE",
                                Publisher = reader["Publisher"]?.ToString()
                            };

                            Books.Add(model);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading books:\n{ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateYearSlider()
        {
            var years = Books.Where(b => b.Year.HasValue).Select(b => b.Year.Value).ToList();
            earliestYear = years.Any() ? years.Min() : 1970;
            latestYear = years.Any() ? years.Max() : DateTime.Now.Year;

            YearSlider.Minimum = 0;
            YearSlider.Maximum = latestYear - earliestYear + 1;
            YearSlider.Value = 0;

            UpdateYearLabel();
        }

        private int SliderValueToYear(int sliderValue) => sliderValue == 0 ? 0 : earliestYear + (sliderValue - 1);

        private IEnumerable<CheckBox> GetAllCheckBoxes(Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is CheckBox cb) yield return cb;
                if (child is Panel inner) foreach (var innerCb in GetAllCheckBoxes(inner)) yield return innerCb;
            }
        }

        private bool FilterBooks(object item)
        {
            if (!(item is BookModel book)) return false;

            string text = (SearchBox.Text ?? "").Trim().ToLower();
            if (!string.IsNullOrEmpty(text))
            {
                bool match =
                       book.ID.ToString().Contains(text)
                    || (book.Author ?? "").ToLower().Contains(text)
                    || (book.Title ?? "").ToLower().Contains(text)
                    || (book.Publisher ?? "").ToLower().Contains(text)
                    || (book.Category ?? "").ToLower().Contains(text)
                    || book.ShelfLocation.ToString().Contains(text)
                    || (book.Year?.ToString() ?? "").Contains(text)
                    || (book.Status ?? "").ToLower().Contains(text);
                if (!match) return false;
            }

            var selectedCategories = GetAllCheckBoxes(CategoryPanel)
                .Where(cb => cb.IsChecked == true)
                .Select(cb => cb.Content?.ToString()?.Trim().ToLower())
                .ToList();

            if (selectedCategories.Any() && !selectedCategories.Contains((book.Category ?? "").Trim().ToLower()))
                return false;

            var selectedStatus = GetAllCheckBoxes(StatusPanel)
                .Where(cb => cb.IsChecked == true)
                .Select(cb => cb.Content?.ToString()?.Trim().ToUpper())
                .ToList();

            if (selectedStatus.Any() && !selectedStatus.Contains(book.Status?.ToUpper() ?? ""))
                return false;

            int sliderYear = SliderValueToYear((int)YearSlider.Value);
            if (sliderYear != 0 && (!book.Year.HasValue || book.Year.Value != sliderYear))
                return false;

            return true;
        }

        private void OnFilterTextChanged(object sender, TextChangedEventArgs e) => _view?.Refresh();
        private void OnFilterCheckedChanged(object sender, RoutedEventArgs e) => _view?.Refresh();
        private void OnYearSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateYearLabel();
            _view?.Refresh();
        }

        private void UpdateYearLabel()
        {
            int year = SliderValueToYear((int)YearSlider.Value);
            YearValue.Text = year == 0 ? "" : year.ToString();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(new LibraLibraryManagementSystem.Controls.BtnAddBook_Page());
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(BooksDataGrid.SelectedItem is BookModel selected))
            {
                MessageBox.Show("Select a book first.", "Edit", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            NavigateToPage(new LibraLibraryManagementSystem.Controls.BtnEditBook(selected));
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(BooksDataGrid.SelectedItem is BookModel selected))
            {
                MessageBox.Show("Select a book to delete.", "Delete", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"Delete \"{selected.Title}\"?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "DELETE FROM dbo.BookModel WHERE ID = @ID";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", selected.ID);
                        cmd.ExecuteNonQuery();
                    }
                }

                Books.Remove(selected);
                _view?.Refresh();
                UpdateYearSlider();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting book:\n{ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavigateToPage(Page target)
        {
            if (NavigationService != null) { NavigationService.Navigate(target); return; }
            if (Application.Current.MainWindow is MainWindow main && main.fContainer != null) { main.fContainer.Navigate(target); return; }
            MessageBox.Show("Unable to navigate.", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public IEnumerable<string> StatusOptions => new string[] { "AVAILABLE", "BORROWED", "PENDING SHELVING" };
    }
}
