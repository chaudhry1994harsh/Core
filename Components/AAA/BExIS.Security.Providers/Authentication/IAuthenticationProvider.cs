﻿using BExIS.Security.Entities.Subjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BExIS.Security.Providers.Authentication
{
    public interface IAuthenticationProvider
    {
        // Should be part of the user management provider
        //User GetUser(string userName, string password);

        // Registration is not necessary at all. Just use the IsUserAuthenticated function
        //User RegisterUser(string userName, string password);

        bool ValidateUser(string userName, string password);
    }
}