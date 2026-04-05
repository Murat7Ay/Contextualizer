use std::collections::HashMap;

#[derive(Debug, Clone)]
pub struct MssqlConnectionConfig {
    pub host: String,
    pub database: String,
    pub username: Option<String>,
    pub password: Option<String>,
    pub port: u16,
}

pub fn parse_mssql_connection_string(cs: &str) -> Result<MssqlConnectionConfig, String> {
    let mut params: HashMap<String, String> = HashMap::new();

    for part in cs.split(';') {
        let trimmed = part.trim();
        if trimmed.is_empty() {
            continue;
        }
        if let Some(eq_pos) = trimmed.find('=') {
            let key = trimmed[..eq_pos].trim().to_lowercase();
            let value = trimmed[eq_pos + 1..].trim().to_string();
            params.insert(key, value);
        }
    }

    let host = params
        .get("server")
        .or_else(|| params.get("data source"))
        .or_else(|| params.get("host"))
        .ok_or("Missing server/host in connection string")?
        .clone();

    let database = params
        .get("database")
        .or_else(|| params.get("initial catalog"))
        .ok_or("Missing database in connection string")?
        .clone();

    let username = params
        .get("user id")
        .or_else(|| params.get("uid"))
        .or_else(|| params.get("user"))
        .cloned();

    let password = params
        .get("password")
        .or_else(|| params.get("pwd"))
        .cloned();

    let port = params
        .get("port")
        .and_then(|p| p.parse().ok())
        .unwrap_or(1433);

    Ok(MssqlConnectionConfig {
        host,
        database,
        username,
        password,
        port,
    })
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_mssql_connection_string_parse() {
        let cs = "Server=localhost;Database=test;User Id=sa;Password=pass;";
        let config = parse_mssql_connection_string(cs).unwrap();
        assert_eq!(config.host, "localhost");
        assert_eq!(config.database, "test");
        assert_eq!(config.username, Some("sa".to_string()));
        assert_eq!(config.password, Some("pass".to_string()));
        assert_eq!(config.port, 1433);
    }

    #[test]
    fn test_invalid_connection_string() {
        assert!(parse_mssql_connection_string("invalid").is_err());
    }

    #[test]
    fn test_connection_string_with_port() {
        let cs = "Server=db.example.com;Database=mydb;Port=5433;";
        let config = parse_mssql_connection_string(cs).unwrap();
        assert_eq!(config.port, 5433);
    }

    #[test]
    fn test_alternative_keys() {
        let cs = "Data Source=host1;Initial Catalog=db1;UID=admin;PWD=secret;";
        let config = parse_mssql_connection_string(cs).unwrap();
        assert_eq!(config.host, "host1");
        assert_eq!(config.database, "db1");
        assert_eq!(config.username, Some("admin".to_string()));
    }
}
