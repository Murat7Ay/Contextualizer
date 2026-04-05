use super::context::ClipboardContent;
use super::types::HandlerConfig;
use anyhow::Result;
use std::collections::HashMap;

pub trait Handler: Send + Sync {
    fn type_name(&self) -> &str;
    fn config(&self) -> &HandlerConfig;
    fn can_handle(&self, content: &ClipboardContent) -> bool;
    fn create_context(&self, content: &ClipboardContent) -> Result<HashMap<String, String>>;
}

pub trait Action: Send + Sync {
    fn name(&self) -> &str;
    fn execute(
        &self,
        action_config: &super::types::ConfigAction,
        context: &HashMap<String, String>,
    ) -> Result<()>;
}
