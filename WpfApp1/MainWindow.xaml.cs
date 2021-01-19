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

        private void StartButton_OnClickButton_Click(object sender, RoutedEventArgs e)
        {
            string userName = (bool)EnterUser.IsChecked ? UserName.Text : Environment.UserName;
            string password = (bool)EnterUser.IsChecked ? Password.Password : "";


            PrincipalContext principalContext = new PrincipalContext(DomainContext.IsChecked != null && (bool)DomainContext.IsChecked ? ContextType.Domain : ContextType.Machine);

            IActiveDirectoryProvider adProvider = new ActiveDirectoryProvider(principalContext);

            var isAuthenticated = false;

            if ((!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password)) || (bool)CurrentUser.IsChecked)
            {
                var user = adProvider.FindUserByName(userName);

                if (user != null)
                {
                    isAuthenticated = (bool)CurrentUser.IsChecked ? true : adProvider.ValidateUserCredentials(userName, password);

                    if (isAuthenticated)
                    {
                        var userGroups = adProvider.GetUserSecurityGroups(user.Name);

                        DomainName.Text = user.Context.Name;
                        Server.Text = user.Context.ConnectedServer;

                        elements.ItemsSource = userGroups;

                        // Create new ClaimsIdentity using the security groups information ...
                    }
                } 
            }
            else
            {
                MessageBox.Show("Insert username and password!");
            }
        }
    }

    public interface IActiveDirectoryProvider
    {
        UserPrincipal FindUserByName(string userName);

        bool ValidateUserCredentials(string userName, string password);

        List<Principal> GetUserSecurityGroups(string userName);
    }

    public class ActiveDirectoryProvider : IActiveDirectoryProvider
    {
        private PrincipalContext _principalContext;

        public ActiveDirectoryProvider(PrincipalContext principalContext)
        {
            _principalContext = principalContext;
        }

        public UserPrincipal FindUserByName(string userName)
        {
            return UserPrincipal.FindByIdentity(_principalContext, userName);
        }

        public bool ValidateUserCredentials(string userName, string password)
        {
            return _principalContext.ValidateCredentials(userName, password);
        }

        public List<Principal> GetUserSecurityGroups(string userName)
        {
            var userPrincipal = UserPrincipal.FindByIdentity(_principalContext, userName);
            List<string> userData = new List<string>();
            if (userPrincipal == null)
            {
                throw new InvalidOperationException("User does not exist.");
            }

            return userPrincipal.GetGroups().ToList();

            //DirectoryEntry de = userPrincipal.GetUnderlyingObject() as DirectoryEntry;
            //if (de.Properties.Contains("memberof"))
            //{
            //    foreach (var dn in de.Properties["memberof"])
            //    {
            //        userData.Add(dn.ToString().Split(',')[0].Split('=')[1]);
            //    }
            //}

            //return userData;
        }
    }
}
