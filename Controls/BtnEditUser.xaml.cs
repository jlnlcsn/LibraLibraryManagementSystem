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
    public partial class BtnEditUser : Page
    {
        private readonly string _connectionString;
        private readonly StudentUser _studentUser;
        private readonly AdminUser _adminUser;
        private readonly string _originalSchoolID;
        private readonly bool _isAdminMode;

        // Constructor for StudentUser
        public BtnEditUser(StudentUser studentUser)
        {
            InitializeComponent();

            _connectionString = ConfigurationManager.ConnectionStrings["LibraDb"]?.ConnectionString;
            _studentUser = studentUser ?? throw new ArgumentNullException(nameof(studentUser));
            _isAdminMode = false;

            _originalSchoolID = studentUser.SchoolID;

            var freshStudentData = GetStudentUserFromDb(_originalSchoolID);
            if (freshStudentData != null)
                LoadStudentUser(freshStudentData);
            else
                MessageBox.Show("Could not load student user data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            SizeChanged += OnSizeChanged;
        }

        // Constructor for AdminUser
        public BtnEditUser(AdminUser adminUser)
        {
            InitializeComponent();

            _connectionString = ConfigurationManager.ConnectionStrings["LibraDb"]?.ConnectionString;
            _adminUser = adminUser ?? throw new ArgumentNullException(nameof(adminUser));
            _isAdminMode = true;

            _originalSchoolID = adminUser.SchoolID;

            var freshAdminData = GetAdminUserFromDb(_originalSchoolID);
            if (freshAdminData != null)
                LoadAdminUser(freshAdminData);
            else
                MessageBox.Show("Could not load admin user data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            SizeChanged += OnSizeChanged;
        }

        private void LoadStudentUser(StudentUser user)
        {
            txtSchoolID.Text = user.SchoolID;
            txtSchoolID.IsEnabled = false;

            txtName.Text = user.Name;
            txtEmail.Text = user.Email;
            txtContactNo.Text = user.ContactNo;
            txtGradeSection.Text = user.GradeSection;
            txtGradeSection.Visibility = Visibility.Visible;
            txtPassword.Password = user.Password;
        }

        private void LoadAdminUser(AdminUser user)
        {
            txtSchoolID.Text = user.SchoolID;
            txtSchoolID.IsEnabled = false;

            txtName.Text = user.Name;
            txtEmail.Text = user.Email;
            txtContactNo.Text = user.ContactNo;

            // Hide GradeSection for admin users
            if (txtGradeSection != null)
                txtGradeSection.Visibility = Visibility.Collapsed;

            txtPassword.Password = user.Password;
        }

        #region Validation Helpers

        private bool IsEmailValid(string email)
        {
            return !string.IsNullOrWhiteSpace(email);
        }

        private bool IsContactNumberValid(string contact)
        {
            if (string.IsNullOrWhiteSpace(contact)) return false;
            var digits = Regex.Replace(contact, @"\D", "");
            return digits.Length >= 7 && digits.Length <= 15;
        }

        private bool IsPasswordValid(string pwd)
        {
            return !string.IsNullOrWhiteSpace(pwd) && pwd.Length >= 6;
        }

        #endregion

        private void CancelUser_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
                NavigationService.GoBack();
            else
                ClearForm();
        }

        private void ClearForm()
        {
            txtSchoolID.Text = "";
            txtName.Text = "";
            txtEmail.Text = "";
            txtContactNo.Text = "";
            if (txtGradeSection != null)
                txtGradeSection.Text = "";
            txtPassword.Password = "";
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            string schoolId = _originalSchoolID;
            string name = txtName.Text.Trim();
            string email = txtEmail.Text.Trim();
            string contact = txtContactNo.Text.Trim();
            string password = txtPassword.Password;

            if (_isAdminMode)
            {
                // Admin user validation
                if (string.IsNullOrWhiteSpace(name) ||
                    !IsEmailValid(email) ||
                    !IsContactNumberValid(contact) ||
                    !IsPasswordValid(password))
                {
                    MessageBox.Show("Please fill all required fields correctly.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    using (SqlConnection conn = new SqlConnection(_connectionString))
                    {
                        conn.Open();

                        using (SqlCommand cmd = new SqlCommand(
                            @"UPDATE AdminUser
                              SET Name = @Name,
                                  Email = @Email,
                                  ContactNo = @ContactNo,
                                  Password = @Password
                              WHERE SchoolID = @SchoolID", conn))
                        {
                            cmd.Parameters.AddWithValue("@SchoolID", schoolId);
                            cmd.Parameters.AddWithValue("@Name", name);
                            cmd.Parameters.AddWithValue("@Email", email);
                            cmd.Parameters.AddWithValue("@ContactNo", contact);
                            cmd.Parameters.AddWithValue("@Password", password);

                            int rows = cmd.ExecuteNonQuery();

                            if (rows > 0)
                            {
                                MessageBox.Show("Admin user updated successfully.", "Success");
                            }
                            else
                            {
                                MessageBox.Show("Update failed. Admin not found.", "Error");
                            }
                        }
                    }

                    NavigationService?.GoBack();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Database error: {ex.Message}", "Error");
                }
            }
            else
            {
                // Student user validation
                string gradeSection = txtGradeSection.Text.Trim();

                if (string.IsNullOrWhiteSpace(name) ||
                    !IsEmailValid(email) ||
                    !IsContactNumberValid(contact) ||
                    !IsPasswordValid(password) ||
                    string.IsNullOrWhiteSpace(gradeSection))
                {
                    MessageBox.Show("Please fill all required fields correctly.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    using (SqlConnection conn = new SqlConnection(_connectionString))
                    {
                        conn.Open();

                        using (SqlCommand cmd = new SqlCommand(
                            @"UPDATE StudentUser
                              SET Name = @Name,
                                  Email = @Email,
                                  ContactNo = @ContactNo,
                                  GradeSection = @GradeSection,
                                  Password = @Password
                              WHERE SchoolID = @SchoolID", conn))
                        {
                            cmd.Parameters.AddWithValue("@SchoolID", schoolId);
                            cmd.Parameters.AddWithValue("@Name", name);
                            cmd.Parameters.AddWithValue("@Email", email);
                            cmd.Parameters.AddWithValue("@ContactNo", contact);
                            cmd.Parameters.AddWithValue("@GradeSection", gradeSection);
                            cmd.Parameters.AddWithValue("@Password", password);

                            int rows = cmd.ExecuteNonQuery();

                            if (rows > 0)
                            {
                                MessageBox.Show("Student user updated successfully.", "Success");
                            }
                            else
                            {
                                MessageBox.Show("Update failed. Student not found.", "Error");
                            }
                        }
                    }

                    NavigationService?.GoBack();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Database error: {ex.Message}", "Error");
                }
            }
        }

        private void TxtName_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Optional: Live validation feedback
        }

        #region Database Helper Methods

        private StudentUser GetStudentUserFromDb(string schoolId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = new SqlCommand(
                    @"SELECT SchoolID, GradeSection, Name, Email, ContactNo, Password
                      FROM StudentUser
                      WHERE SchoolID = @SchoolID", conn))
                {
                    cmd.Parameters.AddWithValue("@SchoolID", schoolId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new StudentUser
                            {
                                SchoolID = reader["SchoolID"].ToString(),
                                GradeSection = reader["GradeSection"].ToString(),
                                Name = reader["Name"].ToString(),
                                Email = reader["Email"].ToString(),
                                ContactNo = reader["ContactNo"].ToString(),
                                Password = reader["Password"].ToString()
                            };
                        }
                    }
                }
            }

            return null;
        }

        private AdminUser GetAdminUserFromDb(string schoolId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = new SqlCommand(
                    @"SELECT SchoolID, Name, Email, ContactNo, Password
                      FROM AdminUser
                      WHERE SchoolID = @SchoolID", conn))
                {
                    cmd.Parameters.AddWithValue("@SchoolID", schoolId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new AdminUser
                            {
                                SchoolID = reader["SchoolID"].ToString(),
                                Name = reader["Name"].ToString(),
                                Email = reader["Email"].ToString(),
                                ContactNo = reader["ContactNo"].ToString(),
                                Password = reader["Password"].ToString()
                            };
                        }
                    }
                }
            }

            return null;
        }

        #endregion

        #region Scaling

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateScale();
        }

        private void UpdateScale()
        {
            // Base dimensions
            double baseWidth = 800;
            double baseHeight = 450;

            // Ensure ActualWidth/Height are nonzero
            double actualW = ActualWidth <= 0 ? baseWidth : ActualWidth;
            double actualH = ActualHeight <= 0 ? baseHeight : ActualHeight;

            double scaleX = actualW / baseWidth;
            double scaleY = actualH / baseHeight;

            double finalScale = Math.Min(scaleX, scaleY);

            // PageScale is the ScaleTransform defined in XAML (Grid.RenderTransform)
            try
            {
                if (FindName("PageScale") is System.Windows.Media.ScaleTransform pageScale)
                {
                    pageScale.ScaleX = finalScale;
                    pageScale.ScaleY = finalScale;
                }
            }
            catch
            {
                // Ignore if PageScale not ready
            }
        }

        #endregion
    }
}