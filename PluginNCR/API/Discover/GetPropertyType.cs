using Naveego.Sdk.Plugins;

namespace PluginHubspot.API.Discover
{
    public static partial class Discover
    {
        public static PropertyType GetPropertyType(string dataType)
        {
            return PropertyType.String;
            // switch (dataType)
            // {
            //     case var t when t.Contains("timestamp"):
            //         return PropertyType.Datetime;
            //     case "date":
            //         return PropertyType.Date;
            //     case "time":
            //         return PropertyType.Time;
            //     case "smallint":
            //     case "int":
            //     case "integer":
            //     case "bigint":
            //         return PropertyType.Integer;
            //     case "decimal":
            //         return PropertyType.Decimal;
            //     case "real":
            //     case "float":
            //     case "double":
            //         return PropertyType.Float;
            //     case "boolean":
            //     case "bit":
            //         return PropertyType.Bool;
            //     case "blob":
            //     case "mediumblob":
            //     case "longblob":
            //         return PropertyType.Blob;
            //     case "char":
            //     case "character":
            //     case "varchar":
            //     case "tinytext":
            //         return PropertyType.String;
            //     case "text":
            //     case "mediumtext":
            //     case "longtext":
            //         return PropertyType.Text;
            //     default:
            //         return PropertyType.String;
            // }
        }
    }
}