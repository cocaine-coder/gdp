namespace GDP.Postgis.Model;

public class Table
{
    public string table_catalog { get; set; }

    public string table_schema { get; set; }

    public string table_name { get; set; }

    public string table_type { get; set; }

    public string self_referencing_column_name { get; set; }

    public string reference_generation { get; set; }

    public string user_defined_type_catalog { get; set; }

    public string user_defined_type_schema { get; set; }

    public string user_defined_type_name { get; set; }

    public string is_insertable_into { get; set; }

    public string is_typed { get; set; }

    public string commit_action { get; set; }
}