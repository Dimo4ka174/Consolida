namespace DB.Authorization
{
    public static class Permissions
    {
        public static class Roles
        {
            public const string Read = "read_roles";
            public const string Create = "create_roles";
            public const string Edit = "edit_roles";
            public const string Delete = "delete_roles";
            public static readonly string[] All = { Read, Create, Edit, Delete };
        }

        public static class Users
        {
            public const string Read = "read_users";
            public const string Create = "create_users";
            public const string Edit = "edit_users";
            public const string Delete = "delete_users";
            public static readonly string[] All = { Read, Create, Edit, Delete };
        }

        public static class Cities
        {
            public const string Read = "read_city";
            public const string Create = "create_city";
            public const string Edit = "edit_city";
            public const string Delete = "delete_city";
            public static readonly string[] All = { Read, Create, Edit, Delete };
        }

        public static class Companies
        {
            public const string Read = "read_companies";
            public const string Create = "create_companies";
            public const string Edit = "edit_companies";
            public const string Delete = "delete_companies";
            public static readonly string[] All = { Read, Create, Edit, Delete };
        }

        public static class Customers
        {
            public const string Read = "read_customer";
            public const string Create = "create_customer";
            public const string Edit = "edit_customer";
            public const string Delete = "delete_customer";
            public static readonly string[] All = { Read, Create, Edit, Delete };
        }

        public static class CodesTNVD
        {
            public const string Read = "read_codes_tnvd";
            public const string Create = "create_codes_tnvd";
            public const string Edit = "edit_codes_tnvd";
            public const string Delete = "delete_codes_tnvd";
            public static readonly string[] All = { Read, Create, Edit, Delete };
        }

        public static class Manufacturers
        {
            public const string Read = "read_manufacturers";
            public const string Create = "create_manufacturers";
            public const string Edit = "edit_manufacturers";
            public const string Delete = "delete_manufacturers";
            public static readonly string[] All = { Read, Create, Edit, Delete };
        }

        public static class Products
        {
            public const string Read = "read_products";
            public const string Create = "create_products";
            public const string Edit = "edit_products";
            public const string Delete = "delete_products";
            public static readonly string[] All = { Read, Create, Edit, Delete };
        }

        public static class MeasureUnits
        {
            public const string Read = "read_measure_units";
            public const string Create = "create_measure_units";
            public const string Edit = "edit_measure_units";
            public const string Delete = "delete_measure_units";
            public static readonly string[] All = { Read, Create, Edit, Delete };
        }

        public static class TaxTypes
        {
            public const string Read = "read_tax_types";
            public const string Create = "create_tax_types";
            public const string Edit = "edit_tax_types";
            public const string Delete = "delete_tax_types";
            public static readonly string[] All = { Read, Create, Edit, Delete };
        }

        public static class Orders
        {
            public const string Read = "read_orders";
            public const string Create = "create_orders";
            public const string Edit = "edit_orders";
            public const string Delete = "delete_orders";
            public static readonly string[] All = { Read, Create, Edit, Delete };
        }
    }
}
