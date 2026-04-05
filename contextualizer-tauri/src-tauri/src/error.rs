use serde::Serialize;

#[derive(Debug, thiserror::Error)]
pub enum AppError {
    #[error(transparent)]
    Io(#[from] std::io::Error),

    #[error("JSON error: {0}")]
    Json(#[from] serde_json::Error),

    #[error("Handler not found: {0}")]
    HandlerNotFound(String),

    #[error("Handler validation failed: {0}")]
    HandlerValidation(String),

    #[error("Config error: {0}")]
    Config(String),

    #[error("Plugin error: {0}")]
    Plugin(String),

    #[error("MCP error: {0}")]
    Mcp(String),

    #[error("{0}")]
    General(String),
}

#[derive(Serialize)]
#[serde(tag = "kind", content = "message")]
#[serde(rename_all = "camelCase")]
enum ErrorKind {
    Io(String),
    Json(String),
    HandlerNotFound(String),
    HandlerValidation(String),
    Config(String),
    Plugin(String),
    Mcp(String),
    General(String),
}

impl Serialize for AppError {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: serde::ser::Serializer,
    {
        let msg = self.to_string();
        let kind = match self {
            Self::Io(_) => ErrorKind::Io(msg),
            Self::Json(_) => ErrorKind::Json(msg),
            Self::HandlerNotFound(_) => ErrorKind::HandlerNotFound(msg),
            Self::HandlerValidation(_) => ErrorKind::HandlerValidation(msg),
            Self::Config(_) => ErrorKind::Config(msg),
            Self::Plugin(_) => ErrorKind::Plugin(msg),
            Self::Mcp(_) => ErrorKind::Mcp(msg),
            Self::General(_) => ErrorKind::General(msg),
        };
        kind.serialize(serializer)
    }
}

pub type AppResult<T> = Result<T, AppError>;

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_error_serialization_io() {
        let err = AppError::Io(std::io::Error::new(std::io::ErrorKind::NotFound, "file not found"));
        let json = serde_json::to_string(&err).unwrap();
        assert!(json.contains("\"kind\":\"io\""));
        assert!(json.contains("file not found"));
    }

    #[test]
    fn test_error_serialization_handler_not_found() {
        let err = AppError::HandlerNotFound("my_handler".to_string());
        let json = serde_json::to_string(&err).unwrap();
        assert!(json.contains("\"kind\":\"handlerNotFound\""));
        assert!(json.contains("my_handler"));
    }

    #[test]
    fn test_error_from_io() {
        let io_err = std::io::Error::new(std::io::ErrorKind::PermissionDenied, "denied");
        let app_err: AppError = io_err.into();
        assert!(matches!(app_err, AppError::Io(_)));
        assert!(app_err.to_string().contains("denied"));
    }

    #[test]
    fn test_error_from_json() {
        let json_err = serde_json::from_str::<serde_json::Value>("invalid").unwrap_err();
        let app_err: AppError = json_err.into();
        assert!(matches!(app_err, AppError::Json(_)));
    }

    #[test]
    fn test_all_error_variants_serialize() {
        let errors: Vec<AppError> = vec![
            AppError::Io(std::io::Error::new(std::io::ErrorKind::Other, "io")),
            AppError::Json(serde_json::from_str::<serde_json::Value>("x").unwrap_err()),
            AppError::HandlerNotFound("h".to_string()),
            AppError::HandlerValidation("v".to_string()),
            AppError::Config("c".to_string()),
            AppError::Plugin("p".to_string()),
            AppError::Mcp("m".to_string()),
            AppError::General("g".to_string()),
        ];
        for err in errors {
            let json = serde_json::to_string(&err).unwrap();
            assert!(json.contains("\"kind\""));
            assert!(json.contains("\"message\""));
        }
    }
}
