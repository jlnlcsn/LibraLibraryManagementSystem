using LibraLibraryManagementSystem.Models.StudentUsers;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace LibraLibraryManagementSystem
{
    /// <summary>
    /// Interaction logic for LoginForm.xaml
    /// </summary>
    public partial class LoginForm : Window
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        // Password placeholder handler
        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtPassword.Password))
                tbPasswordPlaceholder.Visibility = Visibility.Visible;
            else
                tbPasswordPlaceholder.Visibility = Visibility.Collapsed;
        }

        // Close button
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Maximize / Restore
        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
                WindowState = WindowState.Maximized;
            else
                WindowState = WindowState.Normal;
        }

        // Minimize
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }



        // ---------------------------------------------------------
        //   STUDENT LOGIN METHOD (DATABASE AUTHENTICATION)
        // ---------------------------------------------------------
        private StudentUser AuthenticateStudent(string email, string password)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["LibraDB"]?.ConnectionString
                                   ?? ConfigurationManager.ConnectionStrings["LibraDb"]?.ConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                MessageBox.Show("Database connection string not found.", "Configuration Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT SchoolID, GradeSection, Name, Email, ContactNo, Password
                    FROM StudentUser
                    WHERE Email = @Email AND Password = @Password
                ";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Password", password);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
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



        // ---------------------------------------------------------
        //   LOGIN BUTTON HANDLER
        // ---------------------------------------------------------
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            var username = (txtUsername.Text ?? string.Empty).Trim();
            var password = (txtPassword.Password ?? string.Empty).Trim();

            // Validate input
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter both username and password.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ADMIN LOGIN (hardcoded)
            if (username == "admin" && password == "admin")
            {
                // Clear any previous student session data
                Application.Current.Properties.Clear();

                var main = new MainWindow();
                main.Show();
                this.Close();
                return;
            }

            if (username == "student" && password == "student")
            {
                Application.Current.Properties.Clear();

                // Open user main window
                var mainUser = new UserMainWindow();
                mainUser.Show();
                this.Close();
                return;
            }


            // STUDENT LOGIN (database)
            var student = AuthenticateStudent(username, password);
            if (student != null)
            {
                // *** THIS IS THE CRITICAL FIX ***
                // Store student information in Application.Current.Properties
                Application.Current.Properties["CurrentStudentSchoolID"] = student.SchoolID;
                Application.Current.Properties["CurrentStudentName"] = student.Name;
                Application.Current.Properties["CurrentStudentEmail"] = student.Email;
                Application.Current.Properties["CurrentStudentGradeSection"] = student.GradeSection;

                // Success → open student user main window
                var mainUser = new UserMainWindow();
                mainUser.Show();
                this.Close();
                return;
            }


            // If none matched
            MessageBox.Show("Invalid username or password.", "Login failed",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}