using LibraLibraryManagementSystem.UserPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LibraLibraryManagementSystem
{
    /// <summary>
    /// Interaction logic for UserMainWindow.xaml
    /// </summary>
    public partial class UserMainWindow : Window
    {
        public UserMainWindow()
        {
            InitializeComponent();
        }

        // Menu button mouse enter/leave handlers - simple visual placeholders
        private void btnBooks_MouseEnter(object sender, MouseEventArgs e)
        {
            Popup.PlacementTarget = btnBooks;
            Popup.Placement = PlacementMode.Right;
            Popup.IsOpen = true;
            Header.PopupText.Text = "Books";
        }

        private void btnBooks_MouseLeave(object sender, MouseEventArgs e)
        {
            Popup.IsOpen = false;
        }

        private void btnTransactions_MouseEnter(object sender, MouseEventArgs e)
        {
            Popup.PlacementTarget = btnTransactions;
            Popup.Placement = PlacementMode.Right;
            Popup.IsOpen = true;
            Header.PopupText.Text = "Transactions";
        }

        private void btnTransactions_MouseLeave(object sender, MouseEventArgs e)
        {
            Popup.IsOpen = false;
        }

        private void btnProfile_MouseEnter(object sender, MouseEventArgs e)
        {
            Popup.PlacementTarget = btnProfile;
            Popup.Placement = PlacementMode.Right;
            Popup.IsOpen = true;
            Header.PopupText.Text = "Edit Profile";
        }

        private void btnProfile_MouseLeave(object sender, MouseEventArgs e)
        {
            Popup.IsOpen = false;
        }

        // Navigation click handlers
        private void btnUserBooks_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                fContainer.Navigate(new UserBooks());
            }
            catch
            {
                // swallow navigation errors for now
            }
        }

        private void btnUserTransactions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                fContainer.Navigate(new UserTransactions());
            }
            catch
            {
            }
        }

        private void btnUserProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                fContainer.Navigate(new UserEditProfile());
            }
            catch
            {
            }
        }

        // MenuItem loaded event (attached to control inside button)
        private void MenuItem_Loaded(object sender, RoutedEventArgs e)
        {
            // placeholder - no action required
        }

        // Window control buttons
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
                WindowState = WindowState.Maximized;
            else
                WindowState = WindowState.Normal;
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
    }
}
