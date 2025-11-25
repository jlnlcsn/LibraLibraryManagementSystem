using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using LibraLibraryManagementSystem.Models.AdminUsers;
using LibraLibraryManagementSystem.Models.StudentUsers;

namespace LibraLibraryManagementSystem.Controls
{
    /// <summary>
    /// Interaction logic for BtnAddUser.xaml
    /// </summary>
    public partial class BtnAddUser : Page
    {
        private readonly string _connectionString;

        public BtnAddUser()
        {
            InitializeComponent();
            _connectionString = ConfigurationManager.ConnectionStrings["LibraDb"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                MessageBox.Show("Connection string 'LibraDb' is missing or empty in App.config.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            rbStandard.IsChecked = true;
            SizeChanged += OnSizeChanged;

            this.txtSchoolID.Text = GetNextStudentId();

        }

        #region Validation Helpers

        private bool IsEmailValid(string email)
        {
            return !string.IsNullOrWhiteSpace(email);

        }

        private bool IsContactNumberValid(string contact)
        {
            if (string.IsNullOrWhiteSpace(contact)) return false;

            // Allow digits, spaces, dashes, parentheses and leading +
            // But require at least 7 digits
            var digitsOnly = Regex.Replace(contact, @"\D", "");
            return digitsOnly.Length >= 7 && digitsOnly.Length <= 15;
        }

        private bool IsSchoolIdValid(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && id.Length <= 50;
        }

        private bool IsPasswordValid(string pwd)
        {
            return !string.IsNullOrWhiteSpace(pwd) && pwd.Length >= 6; // minimal rule: at least 6 chars
        }

        #endregion

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Navigate back to users page (or simply go back)
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
            else
            {
                // If no navigation service available, just clear fields
                ClearForm();
            }
        }

        private void ClearForm()
        {
            txtSchoolID.Text = string.Empty;
            txtName.Text = string.Empty;
            txtEmail.Text = string.Empty;
            txtContactNo.Text = string.Empty;
            txtGradeSection.Text = string.Empty;
            txtPassword.Password = string.Empty;
            rbStandard.IsChecked = true;
            rbAdmin.IsChecked = false;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            // Gather form values
            var schoolId = txtSchoolID.Text?.Trim();
            var name = txtName.Text?.Trim();
            var email = txtEmail.Text?.Trim();
            var contactNo = txtContactNo.Text?.Trim();
            var gradeSection = txtGradeSection.Text?.Trim();
            var password = txtPassword.Password; // PasswordBox — masked input

            var isAdmin = rbAdmin.IsChecked == true;
            var isStudent = rbStandard.IsChecked == true; // treat Standard as Student

            // Validation
            if (!IsSchoolIdValid(schoolId))
            {
                MessageBox.Show("Please enter a valid School ID (max 50 characters).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtSchoolID.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return;
            }

            if (!IsEmailValid(email))
            {
                MessageBox.Show("Please enter a valid email address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return;
            }

            if (!IsContactNumberValid(contactNo))
            {
                MessageBox.Show("Please enter a valid contact number (7-15 digits).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtContactNo.Focus();
                return;
            }

            if (!IsPasswordValid(password))
            {
                MessageBox.Show("Password is required and must be at least 6 characters.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return;
            }

            if (isStudent && string.IsNullOrWhiteSpace(gradeSection))
            {
                MessageBox.Show("Grade and Section is required for Standard/Student users.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtGradeSection.Focus();
                return;
            }

            // Determine target table and insert
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // First, ensure SchoolID does not already exist in the target table
                    if (isAdmin)
                    {
                        if (ExistsInTable(conn, "AdminUser", schoolId))
                        {
                            MessageBox.Show($"An admin with SchoolID '{schoolId}' already exists.", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                            txtSchoolID.Focus();
                            return;
                        }

                        // Insert into AdminUsers
                        using (var cmd = new SqlCommand(
                            @"INSERT INTO AdminUser (Name, Email, ContactNo, Password)
                              VALUES (@Name, @Email, @ContactNo, @Password)", conn))
                        {
                            cmd.Parameters.AddWithValue("@Name", name);
                            cmd.Parameters.AddWithValue("@Email", email);
                            cmd.Parameters.AddWithValue("@ContactNo", contactNo);
                            cmd.Parameters.AddWithValue("@Password", password); // Consider hashing in production

                            int rows = cmd.ExecuteNonQuery();
                            if (rows > 0)
                            {
                                MessageBox.Show("Admin user added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                // Navigate back so Users page reloads its data (Users.xaml.cs listens to Navigated)
                                if (this.NavigationService != null && this.NavigationService.CanGoBack)
                                {
                                    this.NavigationService.GoBack();
                                }
                                else
                                {
                                    ClearForm();
                                }
                                return;
                            }
                            else
                            {
                                MessageBox.Show("Insert failed. No rows affected.", "Insert Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    else if (isStudent)
                    {
                        if (ExistsInTable(conn, "StudentUser", schoolId))
                        {
                            MessageBox.Show($"A student with SchoolID '{schoolId}' already exists.", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                            txtSchoolID.Focus();
                            return;
                        }

                        using (var cmd = new SqlCommand(
                            @"INSERT INTO StudentUser (GradeSection, Name, Email, ContactNo, Password)
                              VALUES (@GradeSection, @Name, @Email, @ContactNo, @Password)", conn))
                        {
                            cmd.Parameters.AddWithValue("@GradeSection", gradeSection ?? string.Empty);
                            cmd.Parameters.AddWithValue("@Name", name);
                            cmd.Parameters.AddWithValue("@Email", email);
                            cmd.Parameters.AddWithValue("@ContactNo", contactNo);
                            cmd.Parameters.AddWithValue("@Password", password); // Consider hashing in production

                            int rows = cmd.ExecuteNonQuery();
                            if (rows > 0)
                            {
                                MessageBox.Show("Student user added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                if (this.NavigationService != null && this.NavigationService.CanGoBack)
                                {
                                    this.NavigationService.GoBack();
                                }
                                else
                                {
                                    ClearForm();
                                }
                                return;
                            }
                            else
                            {
                                MessageBox.Show("Insert failed. No rows affected.", "Insert Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Please select a user type (Standard or Admin).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show($"Database error: {sqlEx.Message}", "DB Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Checks whether a row with the given SchoolID exists in the specified table.
        /// </summary>
        private bool ExistsInTable(SqlConnection conn, string tableName, string schoolId)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(schoolId))
                return false;

            // Ensure tableName is safe: restrict to expected names to avoid SQL injection by table name
            if (!string.Equals(tableName, "AdminUser", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(tableName, "StudentUser", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Invalid table name for ExistsInTable.");
            }

            using (var cmd = new SqlCommand($"SELECT COUNT(1) FROM {tableName} WHERE SchoolID = @SchoolID", conn))
            {
                cmd.Parameters.AddWithValue("@SchoolID", schoolId);
                var obj = cmd.ExecuteScalar();
                if (obj == null || obj == DBNull.Value) return false;

                int count;
                if (int.TryParse(obj.ToString(), out count))
                {
                    return count > 0;
                }

                return false;
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

        private string GetNextStudentId()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Get MAX ID
                    using (var cmd = new SqlCommand(
                        "SELECT ISNULL(MAX(CAST(SchoolID AS INT)), 0) FROM StudentUser", conn))
                    {
                        int maxId = (int)cmd.ExecuteScalar();
                        return (maxId + 1).ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving next Student ID: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return "";
            }
        }

    }
}
