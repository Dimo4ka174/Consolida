using Microsoft.AspNetCore.Authorization;

namespace DB.Authorization
{
    public static class AuthorizationPolicies
    {
        public static void Configure(AuthorizationOptions options)
        {
            AddRole(options);
            AddUser(options);
            AddCity(options);
            AddCompany(options);
            AddCustomer(options);
        }
        private static void AddRole(AuthorizationOptions options)
        {
            options.AddPolicy("RoleManager", policy => policy.RequireClaim("Permission", Permissions.Roles.All));
            options.AddPolicy("ReadRoles", policy => policy.RequireClaim("Permission", Permissions.Roles.Read));
            options.AddPolicy("CreateRoles", policy => policy.RequireClaim("Permission", Permissions.Roles.Create));
            options.AddPolicy("EditRoles", policy => policy.RequireClaim("Permission", Permissions.Roles.Edit));
            options.AddPolicy("DeleteRoles", policy => policy.RequireClaim("Permission", Permissions.Roles.Delete));
        }

        private static void AddUser(AuthorizationOptions options)
        {
            options.AddPolicy("UserManager", policy => policy.RequireClaim("Permission", Permissions.Users.All));
            options.AddPolicy("ReadUsers", policy => policy.RequireClaim("Permission", Permissions.Users.Read));
            options.AddPolicy("CreateUsers", policy => policy.RequireClaim("Permission", Permissions.Users.Create));
            options.AddPolicy("EditUsers", policy => policy.RequireClaim("Permission", Permissions.Users.Edit));
            options.AddPolicy("DeleteUsers", policy => policy.RequireClaim("Permission", Permissions.Users.Delete));
        }

        private static void AddCity(AuthorizationOptions options)
        {
            options.AddPolicy("CityManager", policy => policy.RequireClaim("Permission", Permissions.Cities.All));
            options.AddPolicy("ReadCities", policy => policy.RequireClaim("Permission", Permissions.Cities.Read));
            options.AddPolicy("CreateCities", policy => policy.RequireClaim("Permission", Permissions.Cities.Create));
            options.AddPolicy("EditCities", policy => policy.RequireClaim("Permission", Permissions.Cities.Edit));
            options.AddPolicy("DeleteCities", policy => policy.RequireClaim("Permission", Permissions.Cities.Delete));
        }

        private static void AddCompany(AuthorizationOptions options)
        {
            options.AddPolicy("CompanyManager", policy => policy.RequireClaim("Permission", Permissions.Companies.All));
            options.AddPolicy("ReadCompanies", policy => policy.RequireClaim("Permission", Permissions.Companies.Read));
            options.AddPolicy("CreateCompanies", policy => policy.RequireClaim("Permission", Permissions.Companies.Create));
            options.AddPolicy("EditCompanies", policy => policy.RequireClaim("Permission", Permissions.Companies.Edit));
            options.AddPolicy("DeleteCompanies", policy => policy.RequireClaim("Permission", Permissions.Companies.Delete));
        }

        private static void AddCustomer(AuthorizationOptions options)
        {
            options.AddPolicy("CustomerManager", policy => policy.RequireClaim("Permission", Permissions.Customers.All));
            options.AddPolicy("ReadCustomers", policy => policy.RequireClaim("Permission", Permissions.Customers.Read));
            options.AddPolicy("CreateCustomers", policy => policy.RequireClaim("Permission", Permissions.Customers.Create));
            options.AddPolicy("EditCustomers", policy => policy.RequireClaim("Permission", Permissions.Customers.Edit));
            options.AddPolicy("DeleteCustomers", policy => policy.RequireClaim("Permission", Permissions.Customers.Delete));
        }
    }
}
