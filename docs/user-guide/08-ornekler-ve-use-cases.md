# Contextualizer - Örnekler ve Use Cases

## 📋 İçindekiler
- [Regex Handler Örnekleri](#regex-handler-örnekleri)
- [Database Handler Örnekleri](#database-handler-örnekleri)
- [API Handler Örnekleri](#api-handler-örnekleri)
- [Custom Handler Örnekleri](#custom-handler-örnekleri)
- [Cron Handler Örnekleri](#cron-handler-örnekleri)
- [Manual Handler Örnekleri](#manual-handler-örnekleri)
- [Kompleks Senaryolar](#kompleks-senaryolar)

---

## Regex Handler Örnekleri

### 1. Email Yakalama ve İşleme

```json
{
  "type": "Regex",
  "name": "Email Processor",
  "pattern": "(?<email>[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,})",
  "output_format": "**Email**: $(email)\\n**Domain**: $func:{{ $(email) | string.split(@) | array.get(1) }}",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output",
      "seeder": {
        "domain": "$func:{{ $(email) | string.split(@) | array.get(1) | string.upper }}"
      },
      "inner_actions": [
        {
          "name": "copytoclipboard",
          "key": "email"
        }
      ]
    }
  ]
}
```

### 2. Sipariş Numarası Takibi

```json
{
  "type": "Regex",
  "name": "Order Tracker",
  "pattern": "(?<order_id>ORD-\\d{6})",
  "requires_confirmation": false,
  "output_format": "# Sipariş Detayları\\n\\n**Sipariş No**: $(order_id)\\n**Tarih**: $func:now.format(yyyy-MM-dd HH:mm:ss)",
  "actions": [
    {
      "name": "show_notification",
      "key": "notification_text",
      "constant_seeder": {
        "notification_text": "Sipariş $(order_id) kopyalandı!",
        "_notification_title": "Sipariş",
        "_duration": "5"
      }
    },
    {
      "name": "copytoclipboard",
      "key": "order_id"
    }
  ]
}
```

### 3. Telefon Numarası Formatlama

```json
{
  "type": "Regex",
  "name": "Phone Formatter",
  "pattern": "(?<country>\\+\\d{1,3})?\\s*(?<area>\\d{3})\\s*(?<first>\\d{3})\\s*(?<last>\\d{2,4})",
  "output_format": "Formatted: $(country) ($(area)) $(first)-$(last)",
  "actions": [
    {
      "name": "copytoclipboard",
      "key": "_formatted_output"
    }
  ]
}
```

---

## Database Handler Örnekleri

### 1. Kullanıcı Bilgisi Sorgulama

```json
{
  "type": "Database",
  "name": "User Info Lookup",
  "regex": "(?<user_id>\\d+)",
  "connection_string": "$config:database.connection_string",
  "connector": "mssql",
  "query": "SELECT u.id, u.name, u.email, u.status, u.created_at FROM users u WHERE u.id = @user_id",
  "output_format": "# Kullanıcı Bilgileri\\n\\n**ID**: $(id0)\\n**Ad**: $(name0)\\n**Email**: $(email0)\\n**Durum**: $(status0)\\n**Kayıt**: $(created_at0)",
  "screen_id": "markdown2",
  "title": "User: $(name0)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output",
      "conditions": {
        "key": "$(status0)",
        "operator": "equals",
        "value": "active"
      }
    }
  ]
}
```

### 2. Envanter Kontrolü

```json
{
  "type": "Database",
  "name": "Inventory Check",
  "regex": "(?<product_id>PRD-\\d+)",
  "connection_string": "$config:database.warehouse_connection",
  "connector": "mssql",
  "query": "SELECT product_name, quantity, warehouse_location, last_updated FROM inventory WHERE product_id = @product_id",
  "output_format": "**Ürün**: $(product_name0)\\n**Stok**: $(quantity0)\\n**Konum**: $(warehouse_location0)\\n**Güncelleme**: $(last_updated0)",
  "actions": [
    {
      "name": "show_notification",
      "key": "stock_alert",
      "conditions": {
        "key": "$(quantity0)",
        "operator": "less_than",
        "value": "10"
      },
      "seeder": {
        "stock_alert": "⚠️ Düşük stok: $(product_name0) - Sadece $(quantity0) adet kaldı!"
      }
    },
    {
      "name": "copytoclipboard",
      "key": "_formatted_output"
    }
  ]
}
```

### 3. SQL Sonuçlarını Tablo Olarak Gösterme

```json
{
  "type": "Database",
  "name": "Sales Report",
  "connection_string": "$config:database.connection_string",
  "connector": "mssql",
  "query": "SELECT TOP 10 product_name, SUM(quantity) as total_sold, SUM(revenue) as total_revenue FROM sales WHERE sale_date >= DATEADD(day, -30, GETDATE()) GROUP BY product_name ORDER BY total_revenue DESC",
  "screen_id": "markdown2",
  "title": "Sales Report",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

---

## API Handler Örnekleri

### 1. GitHub Kullanıcı Bilgisi

```json
{
  "type": "Api",
  "name": "GitHub User Info",
  "regex": "(?<username>[a-zA-Z0-9-]+)",
  "url": "https://api.github.com/users/$(username)",
  "method": "GET",
  "output_format": "# GitHub: $(data.login)\\n\\n**Name**: $(data.name)\\n**Bio**: $(data.bio)\\n**Followers**: $(data.followers)\\n**Repos**: $(data.public_repos)\\n**Profile**: $(data.html_url)",
  "screen_id": "markdown2",
  "title": "GitHub: $(data.login)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

### 2. Weather API ile Hava Durumu

```json
{
  "type": "Api",
  "name": "Weather Info",
  "regex": "(?<city>[a-zA-Z]+)",
  "url": "https://api.openweathermap.org/data/2.5/weather",
  "method": "GET",
  "query_parameters": {
    "q": "$(city)",
    "appid": "$config:api.weather_api_key",
    "units": "metric"
  },
  "output_format": "# Hava Durumu: $(name)\\n\\n**Sıcaklık**: $(main.temp)°C\\n**Hissedilen**: $(main.feels_like)°C\\n**Durum**: $(weather[0].description)\\n**Nem**: $(main.humidity)%",
  "screen_id": "markdown2",
  "title": "Weather: $(name)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

### 3. REST API POST İsteği

```json
{
  "type": "Api",
  "name": "Create User",
  "url": "https://api.example.com/users",
  "method": "POST",
  "headers": {
    "Content-Type": "application/json",
    "Authorization": "Bearer $config:api.token"
  },
  "user_inputs": [
    {
      "key": "user_name",
      "prompt": "Enter name:",
      "required": true
    },
    {
      "key": "user_email",
      "prompt": "Enter email:",
      "required": true,
      "validation_regex": "^[^@]+@[^@]+\\.[^@]+$"
    }
  ],
  "seeder": {
    "request_body": "$func:json.create(name,$(user_name),email,$(user_email),created_at,$func:now.format(yyyy-MM-dd))"
  },
  "body": "$(request_body)",
  "actions": [
    {
      "name": "show_notification",
      "key": "success_msg",
      "seeder": {
        "success_msg": "User created: $(data.name) (ID: $(data.id))"
      }
    }
  ]
}
```

---

## Custom Handler Örnekleri

### 1. JSON Formatter

```json
{
  "type": "Custom",
  "name": "JSON Formatter",
  "validator_name": "jsonvalidator",
  "context_provider_name": "jsonvalidator",
  "screen_id": "json_formatter",
  "title": "JSON",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

### 2. XML Formatter

```json
{
  "type": "Custom",
  "name": "XML Formatter",
  "validator_name": "xmlvalidator",
  "context_provider_name": "xmlvalidator",
  "screen_id": "xml_formatter",
  "title": "XML",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

---

## Cron Handler Örnekleri

### 1. Günlük Rapor

```json
{
  "type": "Cron",
  "name": "Daily Sales Report",
  "cron_expression": "0 9 * * *",
  "actual_type": "Database",
  "connection_string": "$config:database.connection_string",
  "connector": "mssql",
  "query": "SELECT * FROM daily_sales_summary WHERE report_date = CAST(GETDATE() AS DATE)",
  "output_format": "# Günlük Satış Raporu\\n\\nToplam Satış: $(total_sales0)\\nToplam Gelir: $(total_revenue0)",
  "screen_id": "markdown2",
  "title": "Daily Report",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    },
    {
      "name": "show_notification",
      "key": "report_notification",
      "constant_seeder": {
        "report_notification": "Günlük rapor hazır!",
        "_notification_title": "Report Ready"
      }
    }
  ]
}
```

### 2. Periyodik API Kontrolü

```json
{
  "type": "Cron",
  "name": "API Health Check",
  "cron_expression": "*/15 * * * *",
  "actual_type": "Api",
  "url": "https://api.example.com/health",
  "method": "GET",
  "actions": [
    {
      "name": "show_notification",
      "key": "alert_message",
      "conditions": {
        "key": "$(status)",
        "operator": "not_equals",
        "value": "healthy"
      },
      "seeder": {
        "alert_message": "⚠️ API sağlık kontrolü başarısız! Durum: $(status)"
      }
    }
  ]
}
```

---

## Manual Handler Örnekleri

### 1. Template Generator

```json
{
  "type": "Manual",
  "name": "Email Template",
  "title": "Generate Email Template",
  "user_inputs": [
    {
      "key": "recipient_name",
      "prompt": "Recipient name:",
      "required": true
    },
    {
      "key": "subject",
      "prompt": "Email subject:",
      "required": true
    },
    {
      "key": "body",
      "prompt": "Email body:",
      "required": true
    }
  ],
  "seeder": {
    "email_template": "To: $(recipient_name)\\nSubject: $(subject)\\nDate: $func:now.format(yyyy-MM-dd)\\n\\n$(body)\\n\\nBest regards,\\n$func:username"
  },
  "output_format": "$(email_template)",
  "actions": [
    {
      "name": "copytoclipboard",
      "key": "_formatted_output"
    },
    {
      "name": "show_notification",
      "key": "success_msg",
      "constant_seeder": {
        "success_msg": "Email template copied!",
        "_notification_title": "Success"
      }
    }
  ]
}
```

### 2. Code Snippet Generator

```json
{
  "type": "Manual",
  "name": "C# Class Generator",
  "title": "Generate C# Class",
  "user_inputs": [
    {
      "key": "class_name",
      "prompt": "Class name:",
      "required": true,
      "validation_regex": "^[A-Z][a-zA-Z0-9]*$"
    },
    {
      "key": "namespace",
      "prompt": "Namespace:",
      "required": true
    }
  ],
  "seeder": {
    "class_code": "using System;\\n\\nnamespace $(namespace)\\n{\\n    public class $(class_name)\\n    {\\n        // Properties\\n        \\n        // Constructor\\n        public $(class_name)()\\n        {\\n        }\\n        \\n        // Methods\\n    }\\n}"
  },
  "output_format": "$(class_code)",
  "actions": [
    {
      "name": "copytoclipboard",
      "key": "_formatted_output"
    }
  ]
}
```

---

## Kompleks Senaryolar

### 1. Multi-Stage Order Processing

```json
{
  "type": "Regex",
  "name": "Order Processor",
  "pattern": "(?<order_id>ORD-\\d{6})",
  "requires_confirmation": true,
  "user_inputs": [
    {
      "key": "notes",
      "prompt": "Add processing notes:",
      "required": false
    }
  ],
  "seeder": {
    "timestamp": "$func:now.format(yyyy-MM-dd HH:mm:ss)",
    "processor": "$func:username"
  },
  "actions": [
    {
      "name": "show_window",
      "key": "order_details",
      "seeder": {
        "order_details": "# Sipariş: $(order_id)\\n\\n**İşlem Zamanı**: $(timestamp)\\n**İşleyen**: $(processor)\\n**Notlar**: $(notes)"
      },
      "inner_actions": [
        {
          "name": "http_post",
          "key": "api_payload",
          "seeder": {
            "api_url": "$config:api.order_processing_url",
            "api_payload": "$func:json.create(order_id,$(order_id),processed_by,$(processor),timestamp,$(timestamp),notes,$(notes))"
          }
        },
        {
          "name": "show_notification",
          "key": "completion_msg",
          "conditions": {
            "key": "$(http_status_code)",
            "operator": "equals",
            "value": "200"
          },
          "constant_seeder": {
            "completion_msg": "Order $(order_id) processed successfully!",
            "_notification_title": "Success",
            "_duration": "7"
          }
        }
      ]
    }
  ]
}
```

### 2. Data Enrichment Pipeline

```json
{
  "type": "Regex",
  "name": "Customer Enrichment",
  "pattern": "(?<customer_id>CUST-\\d{5})",
  "actions": [
    {
      "name": "show_window",
      "key": "enriched_data",
      "seeder": {
        "db_data": "$(ExecuteDatabaseQuery)",
        "api_data": "$(FetchApiData)",
        "enriched_data": "# Customer $(customer_id)\\n\\n## Database Info\\n$(db_data)\\n\\n## External API Info\\n$(api_data)"
      },
      "inner_actions": [
        {
          "name": "save_to_file",
          "key": "enriched_data",
          "seeder": {
            "file_path": "C:\\\\reports\\\\customer_$(customer_id)_$func:now.format(yyyyMMdd).txt"
          }
        }
      ]
    }
  ]
}
```

---

## Sonraki Adımlar

✅ **Örnekler öğrenildi!** Artık:

1. 🐛 [Troubleshooting](09-troubleshooting-ve-faq.md) ile sorun giderme
2. 📖 [Ana README](README.md) ile genel bakış

---

*Bu dokümantasyon Contextualizer v1.0.0 için hazırlanmıştır.*

