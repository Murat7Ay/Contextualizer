# Contextualizer System Flow - Complete Architecture

## 🎯 System Architecture Flow

```mermaid
graph TB
    subgraph "🖥️ User Interface Layer"
        MW[MainWindow]
        SW[SettingsWindow]
        HEW[HandlerExchangeWindow]
        CMW[CronManagerWindow]
        UDW[UserInputDialog]
    end

    subgraph "⚙️ Core Services"
        SM[SettingsService]
        CS[ConfigurationService]
        LS[LoggingService]
        CRS[CronScheduler]
        TM[ThemeManager]
    end

    subgraph "📋 Clipboard Monitoring"
        KH[KeyboardHook]
        CC[ClipboardCapture]
        HM[HandlerManager]
    end

    subgraph "🔧 Handler Types"
        RH[RegexHandler]
        AH[ApiHandler]
        DH[DatabaseHandler]
        FH[FileHandler]
        LH[LookupHandler]
        MH[ManualHandler]
        SH[SyntheticHandler]
        CH[CronHandler]
        CTH[CustomHandler]
    end

    subgraph "🔌 Plugin System"
        PS[PluginService]
        PA[PluginActions]
        PV[PluginValidators]
        PC[PluginContextProviders]
    end

    subgraph "📦 Marketplace"
        FHE[FileHandlerExchange]
        HP[HandlerPackages]
        IT[InstallTemplates]
    end

    subgraph "📁 Configuration Files"
        HJ[handlers.json]
        CI[config.ini]
        SI[secrets.ini]
        AS[appsettings.json]
    end

    subgraph "🔄 Processing Pipeline"
        CP[ContextProcessor]
        FP[FunctionProcessor]
        AC[ActionController]
        OF[OutputFormatter]
    end

    %% Main Flow
    MW --> KH
    KH --> CC
    CC --> HM
    HM --> RH
    HM --> AH
    HM --> DH
    HM --> FH
    HM --> LH
    HM --> CTH

    %% Settings Flow
    SW --> SM
    SW --> CS
    SM --> AS
    CS --> CI
    CS --> SI

    %% Marketplace Flow
    HEW --> FHE
    FHE --> HP
    HP --> IT
    IT --> HJ

    %% Cron Flow
    CMW --> CRS
    CRS --> CH
    CH --> SH

    %% Manual Handler Flow
    MW --> MH
    MH --> UDW
    UDW --> SH

    %% Plugin Flow
    CTH --> PS
    PS --> PA
    PS --> PV
    PS --> PC

    %% Processing Flow
    RH --> CP
    AH --> CP
    DH --> CP
    CP --> FP
    FP --> AC
    AC --> OF

    %% Service Dependencies
    HM --> LS
    HM --> CS
    MW --> TM

    style MW fill:#e1f5fe
    style HM fill:#f3e5f5
    style CS fill:#e8f5e8
    style FHE fill:#fff3e0
    style CRS fill:#fce4ec
```

## 🚀 Handler Execution Flow

```mermaid
sequenceDiagram
    participant U as User
    participant KH as KeyboardHook
    participant HM as HandlerManager
    participant H as Handler
    participant CP as ContextProcessor
    participant FP as FunctionProcessor
    participant CS as ConfigService
    participant A as Actions
    participant UI as UserFeedback

    Note over U,UI: 📋 Clipboard Monitoring Flow

    U->>KH: Ctrl+C (Copy to clipboard)
    KH->>KH: Capture clipboard content
    KH->>HM: OnTextCaptured(ClipboardContent)
    
    Note over HM: 🔄 Parallel Handler Processing
    
    HM->>+H: CanHandleAsync(content)
    H-->>-HM: true/false
    
    alt Handler can process
        HM->>+H: Execute(content)
        
        Note over H,CS: 🔧 Content Processing
        
        H->>CP: ReplaceDynamicValues(content)
        CP->>FP: ProcessFunctions($func:)
        CP->>CS: ProcessConfigPatterns($config:)
        CS-->>CP: Resolved config values
        FP-->>CP: Processed functions
        CP-->>H: Processed content
        
        Note over H,A: ⚡ Action Execution
        
        H->>A: Execute actions
        A->>UI: Show results
        
        H-->>-HM: Execution complete
    end
    
    HM->>UI: Show activity feedback
    UI->>U: Display results
```

## 🎛️ Handler Types & Capabilities

```mermaid
mindmap
  root((Contextualizer Handlers))
    (📝 RegexHandler)
      Pattern Matching
      Group Extraction
      Text Processing
      Log Analysis
    (🌐 ApiHandler)
      HTTP Requests
      REST APIs
      Authentication
      JSON/XML Processing
    (🗄️ DatabaseHandler)
      SQL Queries
      MSSQL/Oracle
      Parameter Binding
      Result Formatting
    (📁 FileHandler)
      File Operations
      Path Detection
      Extension Filtering
      File Opening
    (📊 LookupHandler)
      CSV Processing
      Data Mapping
      Key-Value Lookup
      Reference Tables
    (🖱️ ManualHandler)
      User Triggered
      Input Dialogs
      Step Navigation
      Custom UI
    (⚙️ SyntheticHandler)
      Template Processing
      Content Generation
      Handler Chaining
      Dynamic Input
    (⏰ CronHandler)
      Scheduled Tasks
      Time-based Triggers
      Automated Workflows
      Background Processing
    (🔌 CustomHandler)
      Plugin Integration
      Validators
      Context Providers
      Extensible Actions
```

## 🔧 Configuration System

```mermaid
graph LR
    subgraph "📁 Config Files"
        CI[config.ini<br/>🔓 Public Settings]
        SI[secrets.ini<br/>🔒 Sensitive Data]
        AS[appsettings.json<br/>⚙️ App Settings]
    end

    subgraph "🎛️ Config Service"
        CS[ConfigurationService]
        SR[SetValue/GetValue]
        FF[File Watching]
    end

    subgraph "🔄 Pattern Replacement"
        PC[Pattern: $config:section.key]
        FC[Pattern: $func:method()]
        RC[Regex Context: $()]
    end

    subgraph "🖥️ Settings UI"
        SW[SettingsWindow]
        CP[Config Paths]
        AF[Auto File Creation]
    end

    subgraph "📦 Marketplace Integration"
        TUI[template_user_inputs]
        CT[config_target]
        AI[Auto Install Config]
    end

    %% Connections
    SW --> CS
    CS --> CI
    CS --> SI
    CS --> AS
    
    PC --> CS
    FC --> FP[FunctionProcessor]
    RC --> CP[ContextProcessor]
    
    TUI --> CT
    CT --> CS
    
    style CI fill:#e8f5e8
    style SI fill:#ffebee
    style CS fill:#e1f5fe
    style SW fill:#f3e5f5
```

## 📦 Marketplace & Installation Flow

```mermaid
sequenceDiagram
    participant U as User
    participant HEW as HandlerExchange
    participant FHE as FileHandlerExchange
    participant TUI as TemplateUserInputs
    participant CS as ConfigService
    participant HM as HandlerManager

    Note over U,HM: 📦 Handler Installation Process

    U->>HEW: Browse marketplace
    HEW->>FHE: ListAvailableHandlers()
    FHE-->>HEW: Handler packages
    
    U->>HEW: Select & Install handler
    HEW->>FHE: InstallHandler(id)
    
    Note over FHE,CS: 🔧 Config Processing
    
    FHE->>TUI: Process template_user_inputs
    TUI->>U: Show input dialogs
    U-->>TUI: Provide config values
    
    TUI->>CS: WriteToConfigFile(config_target, value)
    CS->>CI: Write to config.ini
    CS->>SI: Write to secrets.ini
    
    Note over FHE,HM: 📝 Handler Registration
    
    FHE->>HJ: Update handlers.json
    FHE-->>HEW: Installation complete
    HEW->>HM: Reload handlers
    HM->>HJ: Re-read handlers
    
    HEW->>U: Success notification
```

---

Bu sistem mimarisi, Contextualizer'ın tam ekosistemini gösteriyor:

### ✨ Ana Özellikler:
1. **🔄 Real-time clipboard monitoring**
2. **⚡ Parallel handler processing** 
3. **🔧 Comprehensive config system**
4. **📦 Marketplace integration**
5. **⏰ Cron scheduling**
6. **🔌 Plugin extensibility**
7. **🎨 Theme-aware UI**

### 🎯 İş Akışı:
1. Kullanıcı **Ctrl+C** yapar
2. **KeyboardHook** yakalar
3. **HandlerManager** tüm handler'ları **paralel** çalıştırır
4. Her handler **CanHandle** kontrolü yapar
5. Uygun handler'lar **Execute** eder
6. **ContextProcessor** dinamik değerleri resolve eder
7. **Actions** execute edilir
8. **UserFeedback** sonuçları gösterir
