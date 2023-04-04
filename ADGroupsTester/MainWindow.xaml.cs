using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Threading;
using System.Windows.Media.Animation;

namespace ADGroupsTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-us");
            InitializeComponent();
        }

        // This method handles the click event for the StartButton.
        private async void StartButton_OnClickButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the username and password from the input fields or use the current user's information.
            string userName = (bool)EnterUser.IsChecked ? UserName.Text : Environment.UserName;
            string password = (bool)EnterUser.IsChecked ? Password.Password : "";

            // Check if the user has entered a username and password, or if the "CurrentUser" checkbox is checked.
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password) && !(bool)CurrentUser.IsChecked)
            {
                MessageBox.Show("Insert username and password!");
                return;
            }

            // Create a PrincipalContext for either the Domain or the local Machine, depending on the user's choice.
            PrincipalContext principalContext = GetPrincipalContext();

            // Initialize an ActiveDirectoryProvider to interact with Active Directory.
            IActiveDirectoryProvider adProvider = new ActiveDirectoryProvider(principalContext);

            // Check if the "CurrentUser" checkbox is checked, or validate the entered user credentials.
            bool isAuthenticated = IsUserAuthenticated(adProvider, userName, password);

            if (!isAuthenticated)
            {
                MessageBox.Show("Wrong username or password!");
                return;
            }

            // Retrieve and display user information.
            await DisplayUserInfoAsync(adProvider, userName);
        }

        private PrincipalContext GetPrincipalContext()
        {
            return new PrincipalContext(DomainContext.IsChecked != null && (bool)DomainContext.IsChecked ? ContextType.Domain : ContextType.Machine);
        }

        private bool IsUserAuthenticated(IActiveDirectoryProvider adProvider, string userName, string password)
        {
            return (bool)CurrentUser.IsChecked ? true : adProvider.ValidateUserCredentials(userName, password);
        }

        private async Task DisplayUserInfoAsync(IActiveDirectoryProvider adProvider, string userName)
        {
            // Show the wait label.
            workingLabel.Visibility = Visibility.Visible;

            // Run the FindUserByName operation in a separate thread and update the ProgressBar value.
            var user = await Task.Run(() => adProvider.FindUserByName(userName));

            // Run the GetUserSecurityGroups operation in a separate thread and update the ProgressBar value.
            var userGroups = await Task.Run(() => adProvider.GetUserSecurityGroups(user.Name));

            // Display the user's domain and connected server.
            DomainName.Text = user.Context.Name;
            Server.Text = user.Context.ConnectedServer;

            // Bind the userGroups to the elements ItemsSource.
            elements.ItemsSource = userGroups;

            // Hide the wait label.
            workingLabel.Visibility = Visibility.Collapsed;
        }
    }

    // Define the IActiveDirectoryProvider interface for interacting with Active Directory.
    public interface IActiveDirectoryProvider
    {
        UserPrincipal FindUserByName(string userName);

        bool ValidateUserCredentials(string userName, string password);

        List<Principal> GetUserSecurityGroups(string userName);
    }

    // Implement the IActiveDirectoryProvider interface.
    public class ActiveDirectoryProvider : IActiveDirectoryProvider
    {
        // Use a PrincipalContext instance for interaction with the Active Directory.
        private readonly PrincipalContext _principalContext;

        // Initialize the ActiveDirectoryProvider with a given PrincipalContext.
        public ActiveDirectoryProvider(PrincipalContext principalContext)
        {
            _principalContext = principalContext;
        }

        // Find and return a UserPrincipal by a given username.
        public UserPrincipal FindUserByName(string userName)
        {
            return UserPrincipal.FindByIdentity(_principalContext, userName);
        }

        // Validate user credentials against Active Directory.
        public bool ValidateUserCredentials(string userName, string password)
        {
            return _principalContext.ValidateCredentials(userName, password);
        }

        // Get the list of security groups the user is a member of.
        public List<Principal> GetUserSecurityGroups(string userName)
        {
            var userPrincipal = UserPrincipal.FindByIdentity(_principalContext, userName);

            if (userPrincipal == null)
            {
                throw new InvalidOperationException("User does not exist.");
            }

            return userPrincipal.GetGroups().ToList();
        }
    }

}
