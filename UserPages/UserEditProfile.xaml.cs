using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace LibraLibraryManagementSystem.UserPages
{
    public partial class UserEditProfile : Page
    {
        private readonly string connectionString;
        private string currentStudentSchoolID;

        private string originalName;
        private string originalEmail;
        private string originalContactNo;
        private string originalGradeSection;
        private string originalPassword;

        public UserEditProfile()
        {
            InitializeComponent();
            connectionString = ConfigurationManager.ConnectionStrings["LibraDB"]?.ConnectionString
                            ?? ConfigurationManager.ConnectionStrings["LibraDb"]?.ConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                MessageBox.Show(
                    "Database connection string not found in configuration.",
                    "Configuration Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Get logged-in student's SchoolID
            currentStudentSchoolID = GetCurrentStudentSchoolID();

            Loaded += UserEditProfile_Loaded;
            SizeChanged += OnSizeChanged;
        }

        private string GetCurrentStudentSchoolID()
        {
            // Try different possible keys
            if (Application.Current.Properties.Contains("SchoolID"))
                return Application.Current.Properties["SchoolID"]?.ToString();

            if (Application.Current.Properties.Contains("CurrentStudentSchoolID"))
                return Application.Current.Properties["CurrentStudentSchoolID"]?.ToString();

            if (Application.Current.Properties.Contains("StudentSchoolID"))
                return Application.Current.Properties["StudentSchoolID"]?.ToString();

            // Debug: Show what properties are available
            var props = new System.Text.StringBuilder("Available properties:\n");
            foreach (var key in Application.Current.Properties.Keys)
            {
                props.AppendLine($"- {key}: {Application.Current.Properties[key]}");
            }

            MessageBox.Show(
                props.ToString(),
                "Debug: Application Properties",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return null;
        }

        private void UserEditProfile_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(currentStudentSchoolID))
            {
                MessageBox.Show(
                    "Error: SchoolID not found. Please log in again.\n\n" +
                    "Make sure your login code sets:\n" +
                    "Application.Current.Properties[\"SchoolID\"] = schoolIDValue;",
                    "Missing SchoolID",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                NavigationService?.GoBack();
                return;
            }

            LoadStudentProfile();
        }

        /// <summary>
        /// Load current student's profile data from StudentUser table
        /// </summary>
        private void LoadStudentProfile()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT Name, Email, ContactNo, GradeSection, Password
                        FROM StudentUser
                        WHERE SchoolID = @SchoolID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        // Try as string first (most flexible)
                        cmd.Parameters.Add("@SchoolID", SqlDbType.NVarChar, 50).Value = currentStudentSchoolID;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                originalName = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                originalEmail = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                originalContactNo = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                originalGradeSection = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                originalPassword = reader.IsDBNull(4) ? "" : reader.GetString(4);

                                txtName.Text = originalName;
                                txtEmail.Text = originalEmail;
                                txtContactNo.Text = originalContactNo;
                                txtGradeSection.Text = originalGradeSection;
                                txtPassword.Password = originalPassword;
                            }
                            else
                            {
                                MessageBox.Show(
                                    $"Student profile not found for SchoolID: {currentStudentSchoolID}\n\n" +
                                    "Please contact the administrator.",
                                    "Profile Missing",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading profile: {ex.Message}\n\nSchoolID: {currentStudentSchoolID}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        private bool IsValidContactNumber(string contact)
        {
            if (string.IsNullOrWhiteSpace(contact))
                return false;

            string cleaned = Regex.Replace(contact, @"[\s\-\(\)]", "");
            return Regex.IsMatch(cleaned, @"^\d{7,15}$");
        }

        /// <summary>
        /// Save updated profile information
        /// </summary>
        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            // Required field validation
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsValidEmail(txtEmail.Text))
            {
                MessageBox.Show("Please enter a valid email.\nExample: student@school.edu", "Invalid Email", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsValidContactNumber(txtContactNo.Text))
            {
                MessageBox.Show("Invalid contact number.\nExample: 09123456789", "Invalid Contact Number", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Password) || txtPassword.Password.Length < 6)
            {
                MessageBox.Show("Password must be at least 6 characters.", "Invalid Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check for changes
            bool hasChanges =
                txtName.Text.Trim() != originalName ||
                txtEmail.Text.Trim() != originalEmail ||
                txtContactNo.Text.Trim() != originalContactNo ||
                txtGradeSection.Text.Trim() != originalGradeSection ||
                txtPassword.Password != originalPassword;

            if (!hasChanges)
            {
                MessageBox.Show("No changes were made.", "No Changes", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Save changes to your profile?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                        UPDATE StudentUser
                        SET Name = @Name,
                            Email = @Email,
                            ContactNo = @ContactNo,
                            GradeSection = @GradeSection,
                            Password = @Password
                        WHERE SchoolID = @SchoolID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", txtName.Text.Trim());
                        cmd.Parameters.AddWithValue("@Email", txtEmail.Text.Trim());
                        cmd.Parameters.AddWithValue("@ContactNo", txtContactNo.Text.Trim());
                        cmd.Parameters.AddWithValue("@GradeSection", txtGradeSection.Text.Trim());
                        cmd.Parameters.AddWithValue("@Password", txtPassword.Password);
                        cmd.Parameters.Add("@SchoolID", SqlDbType.NVarChar, 50).Value = currentStudentSchoolID;

                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            Application.Current.Properties["CurrentStudentName"] = txtName.Text.Trim();

                            MessageBox.Show(
                                "Profile updated successfully!",
                                "Success",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                            LoadStudentProfile();
                        }
                        else
                        {
                            MessageBox.Show("Update failed. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving profile: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelUser_Click(object sender, RoutedEventArgs e)
        {
            bool hasChanges =
                txtName.Text.Trim() != originalName ||
                txtEmail.Text.Trim() != originalEmail ||
                txtContactNo.Text.Trim() != originalContactNo ||
                txtGradeSection.Text.Trim() != originalGradeSection ||
                txtPassword.Password != originalPassword;

            if (hasChanges)
            {
                if (MessageBox.Show("Discard changes?", "Cancel", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    LoadStudentProfile();
                }
            }
            else
            {
                MessageBox.Show("No changes to discard.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void TxtName_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Optional: add live validation here
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateScale();
        }

        private void UpdateScale()
        {
            double baseWidth = 800;
            double baseHeight = 450;

            double actualW = ActualWidth <= 0 ? baseWidth : ActualWidth;
            double actualH = ActualHeight <= 0 ? baseHeight : ActualHeight;

            double scaleX = actualW / baseWidth;
            double scaleY = actualH / baseHeight;

            double finalScale = Math.Min(scaleX, scaleY);

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
    }
}