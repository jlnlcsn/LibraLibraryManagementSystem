using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Navigation;
using LibraLibraryManagementSystem.Models.AdminUsers;
using LibraLibraryManagementSystem.Models.StudentUsers;

namespace LibraLibraryManagementSystem.Pages
{
    public partial class Users : Page, INotifyPropertyChanged
    {
        public ObservableCollection<AdminUser> AdminUsers { get; } = new ObservableCollection<AdminUser>();
        public ObservableCollection<StudentUser> StudentUsers { get; } = new ObservableCollection<StudentUser>();

        private AdminUser _selectedAdminUser;
        public AdminUser SelectedAdminUser
        {
            get => _selectedAdminUser;
            set { _selectedAdminUser = value; OnPropertyChanged(nameof(SelectedAdminUser)); }
        }

        private StudentUser _selectedStudent;
        public StudentUser SelectedStudent
        {
            get => _selectedStudent;
            set { _selectedStudent = value; OnPropertyChanged(nameof(SelectedStudent)); }
        }

        private enum ActiveTable { Admin, Student }
        private ActiveTable _activeTable = ActiveTable.Student; // default

        private readonly string _connectionString;

        public Users()
        {
            InitializeComponent();
            DataContext = this;

            _connectionString = ConfigurationManager.ConnectionStrings["LibraDb"]?.ConnectionString;

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                MessageBox.Show("Connection string 'LibraDb' missing in App.config", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            this.Loaded += Users_Loaded;

            SearchBox.TextChanged += OnFilterTextChanged;
            SearchBox.GotFocus += (s, e) => SearchPlaceholder.Visibility = Visibility.Collapsed;
            SearchBox.LostFocus += (s, e) =>
            {
                SearchPlaceholder.Visibility = string.IsNullOrWhiteSpace(SearchBox.Text)
                    ? Visibility.Visible : Visibility.Collapsed;
            };

            AdminDataGrid.PreviewMouseLeftButtonDown += (s, e) => _activeTable = ActiveTable.Admin;
            StudentDataGrid.PreviewMouseLeftButtonDown += (s, e) => _activeTable = ActiveTable.Student;

            AdminDataGrid.SelectionChanged += (s, e) =>
            {
                SelectedAdminUser = AdminDataGrid.SelectedItem as AdminUser;
            };

            StudentDataGrid.SelectionChanged += (s, e) =>
            {
                SelectedStudent = StudentDataGrid.SelectedItem as StudentUser;
            };

            if (NavigationService != null)
            {
                NavigationService.Navigated += NavigationService_Navigated;
            }
        }

        private void Users_Loaded(object sender, RoutedEventArgs e)
        {
            AdminDataGrid.CanUserResizeColumns = true;
            AdminDataGrid.ColumnWidth = new DataGridLength(1, DataGridLengthUnitType.Star);

            StudentDataGrid.CanUserResizeColumns = true;
            StudentDataGrid.ColumnWidth = new DataGridLength(1, DataGridLengthUnitType.Star);

            LoadData();
        }

        private void NavigationService_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.Content == this)
                LoadData();
        }

        private void LoadData()
        {
            try
            {
                AdminUsers.Clear();
                StudentUsers.Clear();

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Load AdminUsers
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT SchoolID, Name, Email, ContactNo, Password FROM AdminUser", conn))
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            AdminUsers.Add(new AdminUser
                            {
                                SchoolID = rdr["SchoolID"]?.ToString(),
                                Name = rdr["Name"]?.ToString(),
                                Email = rdr["Email"]?.ToString(),
                                ContactNo = rdr["ContactNo"]?.ToString(),
                                Password = rdr["Password"]?.ToString()
                            });
                        }
                    }

                    // Load StudentUsers
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT SchoolID, GradeSection, Name, Email, ContactNo, Password FROM StudentUser", conn))
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            StudentUsers.Add(new StudentUser
                            {
                                SchoolID = rdr["SchoolID"]?.ToString(),
                                GradeSection = rdr["GradeSection"]?.ToString(),
                                Name = rdr["Name"]?.ToString(),
                                Email = rdr["Email"]?.ToString(),
                                ContactNo = rdr["ContactNo"]?.ToString(),
                                Password = rdr["Password"]?.ToString()
                            });
                        }
                    }
                }

                AdminDataGrid.ItemsSource = AdminUsers;
                StudentDataGrid.ItemsSource = StudentUsers;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading from DB: {ex.Message}");
            }
        }

        private void OnFilterTextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = SearchBox.Text.Trim();
            SearchPlaceholder.Visibility =
                string.IsNullOrWhiteSpace(filter) ? Visibility.Visible : Visibility.Collapsed;

            if (_activeTable == ActiveTable.Admin)
            {
                var view = CollectionViewSource.GetDefaultView(AdminUsers);
                if (string.IsNullOrEmpty(filter))
                    view.Filter = null;
                else
                {
                    view.Filter = obj =>
                    {
                        var u = obj as AdminUser;
                        if (u == null) return false;

                        return (!string.IsNullOrEmpty(u.SchoolID) && u.SchoolID.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                               || (!string.IsNullOrEmpty(u.Name) && u.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                               || (!string.IsNullOrEmpty(u.Email) && u.Email.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                               || (!string.IsNullOrEmpty(u.ContactNo) && u.ContactNo.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                    };
                }

                view.Refresh();
            }
            else
            {
                var view = CollectionViewSource.GetDefaultView(StudentUsers);
                if (string.IsNullOrEmpty(filter))
                    view.Filter = null;
                else
                {
                    view.Filter = obj =>
                    {
                        var u = obj as StudentUser;
                        if (u == null) return false;

                        return (!string.IsNullOrEmpty(u.SchoolID) && u.SchoolID.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                               || (!string.IsNullOrEmpty(u.GradeSection) && u.GradeSection.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                               || (!string.IsNullOrEmpty(u.Name) && u.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                               || (!string.IsNullOrEmpty(u.Email) && u.Email.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                               || (!string.IsNullOrEmpty(u.ContactNo) && u.ContactNo.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                    };
                }

                view.Refresh();
            }
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Controls.BtnAddUser());
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTable == ActiveTable.Admin)
            {
                if (SelectedAdminUser == null)
                {
                    MessageBox.Show("Select an admin first.");
                    return;
                }

                var page = new Controls.BtnEditUser(SelectedAdminUser);
                NavigationService?.Navigate(page);
            }
            else
            {
                if (SelectedStudent == null)
                {
                    MessageBox.Show("Select a student first.");
                    return;
                }

                var page = new Controls.BtnEditUser(SelectedStudent);
                NavigationService?.Navigate(page);
            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_activeTable == ActiveTable.Admin)
                {
                    if (SelectedAdminUser == null)
                    {
                        MessageBox.Show("Select an admin user.");
                        return;
                    }

                    using (SqlConnection conn = new SqlConnection(_connectionString))
                    {
                        conn.Open();
                        SqlCommand cmd = new SqlCommand(
                            "DELETE FROM AdminUser WHERE SchoolID=@id", conn);

                        cmd.Parameters.AddWithValue("@id", SelectedAdminUser.SchoolID);
                        cmd.ExecuteNonQuery();
                    }

                    AdminUsers.Remove(SelectedAdminUser);
                    SelectedAdminUser = null;
                }
                else
                {
                    if (SelectedStudent == null)
                    {
                        MessageBox.Show("Select a student user.");
                        return;
                    }

                    using (SqlConnection conn = new SqlConnection(_connectionString))
                    {
                        conn.Open();
                        SqlCommand cmd = new SqlCommand(
                            "DELETE FROM StudentUser WHERE SchoolID=@id", conn);

                        cmd.Parameters.AddWithValue("@id", SelectedStudent.SchoolID);
                        cmd.ExecuteNonQuery();
                    }

                    StudentUsers.Remove(SelectedStudent);
                    SelectedStudent = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string p)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }
    }
}
