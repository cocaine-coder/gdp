namespace GDP.Postgis.Model;

public class Schemata
{
    public string catalog_name { get; set; }

    public string schema_name { get; set; }

    public string schema_owner { get; set; }

    public string default_character_set_catalog { get; set; }

    public string default_character_set_schema { get; set; }

    public string default_character_set_name { get; set; }

    public string sql_path { get; set; }
}