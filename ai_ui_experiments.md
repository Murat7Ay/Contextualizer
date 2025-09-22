# 🎨 AI Chat UI/UX Experiments

## 📖 İçindekiler
1. [Quick Chat Popup Design](#quick-chat-popup-design)
2. [Full Chat Tab Design](#full-chat-tab-design)
3. [Interactive Components](#interactive-components)
4. [Responsive Behavior](#responsive-behavior)
5. [User Interaction Patterns](#user-interaction-patterns)
6. [Component Architecture](#component-architecture)
7. [Theme Integration](#theme-integration)
8. [Performance Optimizations](#performance-optimizations)

---

## 🔸 Quick Chat Popup Design

### 📱 **Compact Mode (400x600px)**

```
┌─────────────────── AI Quick Chat ──────────────────┐
│ 🤖 Assistant    [📌] [⚙️] [↗️] [❌]              │
├─────────────────────────────────────────────────────┤
│ 📍 Context: AuthService.cs                         │
│ 📋 Clipboard: "async Task ExecuteHandler..."       │
├─────────────────────────────────────────────────────┤
│                                                     │
│ 💬 Hey! I can help you with the AuthService code.  │
│    What would you like me to do?                   │
│                                         [12:34] 🤖 │
│                                                     │
│ User: Can you optimize this code?                   │
│ [12:35] 👤                                         │
│                                                     │
│ 🤖 I'll analyze the code for optimization          │
│    opportunities. Let me check...                  │
│                                                     │
│    🔧 Using tool: read_file                        │
│    ▓▓▓▓▓▓░░ 75% Reading file...                    │
│                                         [12:35] 🤖 │
│                                                     │
│ ┌─ Quick Actions ──────────────────────────────────┐ │
│ │ 💡 Explain Code    🔍 Find Bugs    📝 Add Tests │ │
│ └─────────────────────────────────────────────────┘ │
│                                                     │
├─────────────────────────────────────────────────────┤
│ [📎] [📋] Type your message...          [🎤] [📤] │
└─────────────────────────────────────────────────────┘
```

### 🛠️ **XAML Implementation - Quick Chat**

```xml
<Window x:Class="WpfInteractionApp.AIQuickChatWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="AI Quick Chat" 
        Width="400" Height="600"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        ShowInTaskbar="False"
        Topmost="True">
    
    <Border Background="{DynamicResource Carbon.Brush.Background.Primary}"
            BorderBrush="{DynamicResource Carbon.Brush.Border}"
            BorderThickness="1"
            CornerRadius="12"
            Effect="{StaticResource Carbon.Shadow.Large}">
        
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>      <!-- Header -->
                <RowDefinition Height="Auto"/>      <!-- Context Bar -->
                <RowDefinition Height="*"/>         <!-- Chat Area -->
                <RowDefinition Height="Auto"/>      <!-- Quick Actions -->
                <RowDefinition Height="Auto"/>      <!-- Input Area -->
            </Grid.RowDefinitions>
            
            <!-- Header -->
            <Border Grid.Row="0" 
                    Background="{DynamicResource Carbon.Brush.Background.Secondary}"
                    CornerRadius="12,12,0,0"
                    Padding="16,12">
                <Grid>
                    <StackPanel Orientation="Horizontal">
                        <TextBlock Text="🤖" FontSize="16" VerticalAlignment="Center"/>
                        <TextBlock Text="Assistant" 
                                   Style="{StaticResource Carbon.TextBlock.Base}"
                                   FontWeight="SemiBold"
                                   Margin="8,0,0,0"
                                   VerticalAlignment="Center"/>
                    </StackPanel>
                    
                    <StackPanel Orientation="Horizontal" 
                                HorizontalAlignment="Right">
                        <Button Content="📌" Style="{StaticResource Carbon.Button.IconSmall}" 
                                ToolTip="Pin on Top" Width="28" Height="28" Margin="2"/>
                        <Button Content="⚙️" Style="{StaticResource Carbon.Button.IconSmall}"
                                ToolTip="Settings" Width="28" Height="28" Margin="2"/>
                        <Button Content="↗️" Style="{StaticResource Carbon.Button.IconSmall}"
                                ToolTip="Expand to Tab" Width="28" Height="28" Margin="2"/>
                        <Button Content="❌" Style="{StaticResource Carbon.Button.IconSmall}"
                                ToolTip="Close" Width="28" Height="28" Margin="2"/>
                    </StackPanel>
                </Grid>
            </Border>
            
            <!-- Context Bar -->
            <Border Grid.Row="1"
                    Background="{DynamicResource Carbon.Brush.Background.Tertiary}"
                    Padding="16,8">
                <StackPanel>
                    <StackPanel Orientation="Horizontal" Margin="0,0,0,4">
                        <TextBlock Text="📍" FontSize="12"/>
                        <TextBlock Text="Context: " FontSize="12" Margin="4,0,2,0"/>
                        <TextBlock Text="AuthService.cs" FontSize="12" FontWeight="SemiBold"/>
                    </StackPanel>
                    <StackPanel Orientation="Horizontal">
                        <TextBlock Text="📋" FontSize="12"/>
                        <TextBlock Text="Clipboard: " FontSize="12" Margin="4,0,2,0"/>
                        <TextBlock Text="async Task ExecuteHandler..." 
                                   FontSize="12" 
                                   Foreground="{DynamicResource Carbon.Brush.Text.Secondary}"/>
                    </StackPanel>
                </StackPanel>
            </Border>
            
            <!-- Chat Area -->
            <ScrollViewer Grid.Row="2" 
                          VerticalScrollBarVisibility="Auto"
                          Padding="16">
                <ItemsControl x:Name="MessagesPanel">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <!-- Chat Bubble Template -->
                            <Grid Margin="0,8">
                                <Grid.Style>
                                    <Style TargetType="Grid">
                                        <Style.Triggers>
                                            <DataTrigger Binding="{Binding Role}" Value="user">
                                                <Setter Property="HorizontalAlignment" Value="Right"/>
                                            </DataTrigger>
                                            <DataTrigger Binding="{Binding Role}" Value="assistant">
                                                <Setter Property="HorizontalAlignment" Value="Left"/>
                                            </DataTrigger>
                                        </Style.Triggers>
                                    </Style>
                                </Grid.Style>
                                
                                <Border MaxWidth="280" 
                                        Padding="12,8"
                                        CornerRadius="12,12,4,12">
                                    <Border.Style>
                                        <Style TargetType="Border">
                                            <Style.Triggers>
                                                <DataTrigger Binding="{Binding Role}" Value="user">
                                                    <Setter Property="Background" 
                                                            Value="{StaticResource Carbon.Brush.Info.Primary}"/>
                                                </DataTrigger>
                                                <DataTrigger Binding="{Binding Role}" Value="assistant">
                                                    <Setter Property="Background" 
                                                            Value="{StaticResource Carbon.Brush.Background.Secondary}"/>
                                                    <Setter Property="CornerRadius" Value="12,12,12,4"/>
                                                </DataTrigger>
                                            </Style.Triggers>
                                        </Style>
                                    </Border.Style>
                                    
                                    <TextBlock Text="{Binding Content}"
                                               TextWrapping="Wrap"
                                               FontSize="13">
                                        <TextBlock.Style>
                                            <Style TargetType="TextBlock">
                                                <Style.Triggers>
                                                    <DataTrigger Binding="{Binding Role}" Value="user">
                                                        <Setter Property="Foreground" Value="White"/>
                                                    </DataTrigger>
                                                    <DataTrigger Binding="{Binding Role}" Value="assistant">
                                                        <Setter Property="Foreground" 
                                                                Value="{StaticResource Carbon.Brush.Text.Primary}"/>
                                                    </DataTrigger>
                                                </Style.Triggers>
                                            </Style>
                                        </TextBlock.Style>
                                    </TextBlock>
                                </Border>
                            </Grid>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </ScrollViewer>
            
            <!-- Quick Actions -->
            <Border Grid.Row="3" 
                    Background="{DynamicResource Carbon.Brush.Background.Secondary}"
                    Padding="16,8">
                <WrapPanel HorizontalAlignment="Center">
                    <Button Content="💡 Explain Code" 
                            Style="{StaticResource Carbon.Button.Secondary}"
                            FontSize="11" Padding="8,4" Margin="2"/>
                    <Button Content="🔍 Find Bugs" 
                            Style="{StaticResource Carbon.Button.Secondary}"
                            FontSize="11" Padding="8,4" Margin="2"/>
                    <Button Content="📝 Add Tests" 
                            Style="{StaticResource Carbon.Button.Secondary}"
                            FontSize="11" Padding="8,4" Margin="2"/>
                </WrapPanel>
            </Border>
            
            <!-- Input Area -->
            <Border Grid.Row="4"
                    Background="{DynamicResource Carbon.Brush.Background.Primary}"
                    CornerRadius="0,0,12,12"
                    Padding="16,12">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    
                    <Button Grid.Column="0" Content="📎" 
                            Style="{StaticResource Carbon.Button.IconSmall}"
                            ToolTip="Attach File" Width="28" Height="28"/>
                    <Button Grid.Column="1" Content="📋" 
                            Style="{StaticResource Carbon.Button.IconSmall}"
                            ToolTip="Use Clipboard" Width="28" Height="28" Margin="4,0,8,0"/>
                    
                    <TextBox Grid.Column="2" 
                             x:Name="MessageInput"
                             Style="{StaticResource Carbon.TextBox}"
                             FontSize="13"
                             Height="28"
                             VerticalContentAlignment="Center"
                             Padding="8,4">
                        <TextBox.Resources>
                            <Style TargetType="TextBox" BasedOn="{StaticResource Carbon.TextBox}">
                                <Setter Property="Template">
                                    <Setter.Value>
                                        <ControlTemplate TargetType="TextBox">
                                            <Border Background="{TemplateBinding Background}"
                                                    BorderBrush="{TemplateBinding BorderBrush}"
                                                    BorderThickness="{TemplateBinding BorderThickness}"
                                                    CornerRadius="14">
                                                <Grid>
                                                    <TextBlock Text="Type your message..." 
                                                               Foreground="#999999" 
                                                               FontSize="13"
                                                               Margin="8,0"
                                                               VerticalAlignment="Center"
                                                               IsHitTestVisible="False">
                                                        <TextBlock.Style>
                                                            <Style TargetType="TextBlock">
                                                                <Setter Property="Visibility" Value="Collapsed"/>
                                                                <Style.Triggers>
                                                                    <DataTrigger Binding="{Binding Text, RelativeSource={RelativeSource AncestorType=TextBox}}" Value="">
                                                                        <Setter Property="Visibility" Value="Visible"/>
                                                                    </DataTrigger>
                                                                </Style.Triggers>
                                                            </Style>
                                                        </TextBlock.Style>
                                                    </TextBlock>
                                                    <ScrollViewer x:Name="PART_ContentHost" 
                                                                 Padding="{TemplateBinding Padding}"
                                                                 VerticalAlignment="Center"/>
                                                </Grid>
                                            </Border>
                                        </ControlTemplate>
                                    </Setter.Value>
                                </Setter>
                            </Style>
                        </TextBox.Resources>
                    </TextBox>
                    
                    <Button Grid.Column="3" Content="🎤" 
                            Style="{StaticResource Carbon.Button.IconSmall}"
                            ToolTip="Voice Input" Width="28" Height="28" Margin="8,0,4,0"/>
                    <Button Grid.Column="4" Content="📤" 
                            Style="{StaticResource Carbon.Button.Primary}"
                            ToolTip="Send Message" Width="28" Height="28"/>
                </Grid>
            </Border>
        </Grid>
    </Border>
</Window>
```

---

## 📋 Full Chat Tab Design

### 🖥️ **Desktop Mode (1200x800px)**

```
┌──────────────────────── AI Assistant Chat ─────────────────────────┐
│ [🏠] [🤖 AI Chat] [🛠️ Tools] [⏰ Tasks] [⚙️ Settings]               │
├─────────┬─────────────────────────────────────────────┬─────────────┤
│         │                                             │             │
│ 📋 Chats│ 🤖 GPT-4 Turbo │ [Tools] │ [📋] │ [🔍] │ [❌] │ 📊 Context │
│         ├─────────────────────────────────────────────┤             │
│ ├─Today │                                             │ 📁 Current: │
│ │├─Chat1│ User: Can you optimize this AuthService?    │ AuthSvc.cs  │
│ │├─Chat2│                                    [15:30] 👤│             │
│ │└─Chat3│                                             │ 📋 Clip:    │
│ │       │ 🤖 I'll analyze the code for optimization   │ "async..."  │
│ ├─Yest. │    opportunities. Let me examine it:        │             │
│ │├─Debug│                                             │ 🔧 Tools:   │
│ │└─Review│    🔧 Using tool: read_file                │ • read_file │
│ │       │    📄 Reading AuthService.cs...             │ • grep      │
│ ├─Settings│                                             │ • web_srch  │
│ │       │    ✅ Analysis complete! I found several    │             │
│ ├─[New] │    areas for improvement:                   │ 📊 Usage:   │
│         │                                    [15:31] 🤖│ 247 tokens │
│         │    1. **Async Optimization**: The method    │             │
│         │       currently blocks on database calls   │ 🕒 Response:│
│         │       but could use ConfigureAwait(false)   │ 1.2s avg   │
│         │                                             │             │
│         │    2. **Connection Pooling**: Multiple DB   │ 💰 Cost:    │
│         │       connections created unnecessarily     │ $0.03      │
│         │                                             │             │
│         │    3. **Caching**: User permissions         │             │
│         │       could be cached for 5 minutes        │             │
│         │                                             │             │
│         │    ┌─ Suggested Actions ─────────────────┐  │             │
│         │    │ 🚀 Apply Fixes    📊 Benchmark    │  │             │
│         │    │ 📝 Create Task    🔍 Explain More │  │             │
│         │    └─────────────────────────────────────┘  │             │
│         │                                             │             │
│         │ User: Apply the async optimization fix      │             │
│         │                                    [15:32] 👤│             │
│         │                                             │             │
│         │ 🤖 I'll apply the async optimization fix.   │             │
│         │    This will modify the AuthService.cs file.│             │
│         │                                             │             │
│         │    ⚠️  This operation requires file write   │             │
│         │    permission. Do you want me to proceed?   │             │
│         │                                             │             │
│         │    [✅ Yes, Apply Fix] [❌ Cancel] [👁️ Preview]│             │
│         │                                             │             │
├─────────┴─────────────────────────────────────────────┴─────────────┤
│ 💡 Smart Suggestions:                                                │
│ • "Explain this error message"  • "Generate unit tests"             │
│ • "Review code for security"    • "Create documentation"            │
├───────────────────────────────────────────────────────────────────────┤
│ [📎] [📋] [🔧] Type your message...                    [🎤] [📤]    │
└───────────────────────────────────────────────────────────────────────┘
```

### 🛠️ **XAML Implementation - Full Chat Tab**

```xml
<UserControl x:Class="WpfInteractionApp.AIChatWindow"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Background="{DynamicResource Carbon.Brush.Background.Primary}">
    
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="250" MinWidth="200"/>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*" MinWidth="400"/>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="200" MinWidth="150"/>
        </Grid.ColumnDefinitions>
        
        <!-- Left Sidebar - Chat Sessions -->
        <Border Grid.Column="0" 
                Background="{DynamicResource Carbon.Brush.Background.Secondary}"
                BorderBrush="{DynamicResource Carbon.Brush.Border}"
                BorderThickness="0,0,1,0">
            
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                
                <!-- Sessions Header -->
                <Border Grid.Row="0" Padding="16,12">
                    <Grid>
                        <TextBlock Text="📋 Chat Sessions" 
                                   Style="{StaticResource Carbon.TextBlock.Base}"
                                   FontWeight="SemiBold"/>
                        <Button Content="➕" 
                                Style="{StaticResource Carbon.Button.IconSmall}"
                                HorizontalAlignment="Right"
                                ToolTip="New Chat"
                                Width="24" Height="24"/>
                    </Grid>
                </Border>
                
                <!-- Sessions List -->
                <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto">
                    <TreeView x:Name="SessionsTree" 
                              Style="{StaticResource Carbon.TreeView}"
                              BorderThickness="0"
                              Background="Transparent">
                        
                        <!-- Today Group -->
                        <TreeViewItem Header="📅 Today" IsExpanded="True">
                            <TreeViewItem Header="🔧 Code Optimization" IsSelected="True"/>
                            <TreeViewItem Header="🐛 Bug Investigation"/>
                            <TreeViewItem Header="📝 Documentation"/>
                        </TreeViewItem>
                        
                        <!-- Yesterday Group -->
                        <TreeViewItem Header="📅 Yesterday">
                            <TreeViewItem Header="🔍 Code Review"/>
                            <TreeViewItem Header="⚡ Performance"/>
                        </TreeViewItem>
                        
                        <!-- Settings -->
                        <TreeViewItem Header="⚙️ Settings"/>
                    </TreeView>
                </ScrollViewer>
                
                <!-- Session Actions -->
                <Border Grid.Row="2" Padding="16,8">
                    <StackPanel>
                        <Button Content="📤 Export Chat" 
                                Style="{StaticResource Carbon.Button.Secondary}"
                                HorizontalAlignment="Stretch" Margin="0,2"/>
                        <Button Content="🗑️ Clear All" 
                                Style="{StaticResource Carbon.Button.Secondary}"
                                HorizontalAlignment="Stretch" Margin="0,2"/>
                    </StackPanel>
                </Border>
            </Grid>
        </Border>
        
        <!-- Splitter -->
        <GridSplitter Grid.Column="1" 
                      Width="5" 
                      Background="{DynamicResource Carbon.Brush.Background.Tertiary}"
                      VerticalAlignment="Stretch"/>
        
        <!-- Main Chat Area -->
        <Grid Grid.Column="2">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            
            <!-- Chat Header -->
            <Border Grid.Row="0" 
                    Background="{DynamicResource Carbon.Brush.Background.Secondary}"
                    BorderBrush="{DynamicResource Carbon.Brush.Border}"
                    BorderThickness="0,0,0,1"
                    Padding="20,12">
                <Grid>
                    <StackPanel Orientation="Horizontal">
                        <TextBlock Text="🤖" FontSize="18" VerticalAlignment="Center"/>
                        <TextBlock Text="GPT-4 Turbo" 
                                   Style="{StaticResource Carbon.TextBlock.Base}"
                                   FontSize="16" FontWeight="SemiBold"
                                   Margin="8,0,0,0" VerticalAlignment="Center"/>
                    </StackPanel>
                    
                    <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                        <Button Content="🛠️ Tools" Style="{StaticResource Carbon.Button.Secondary}"
                                Padding="8,4" Margin="4,0"/>
                        <Button Content="📋" Style="{StaticResource Carbon.Button.IconSmall}"
                                ToolTip="Clipboard" Width="32" Height="32" Margin="2"/>
                        <Button Content="🔍" Style="{StaticResource Carbon.Button.IconSmall}"
                                ToolTip="Search" Width="32" Height="32" Margin="2"/>
                        <Button Content="❌" Style="{StaticResource Carbon.Button.IconSmall}"
                                ToolTip="Close" Width="32" Height="32" Margin="2"/>
                    </StackPanel>
                </Grid>
            </Border>
            
            <!-- Messages Area -->
            <ScrollViewer Grid.Row="1" 
                          x:Name="MessagesScrollViewer"
                          VerticalScrollBarVisibility="Auto"
                          Padding="20">
                <ItemsControl x:Name="MessagesPanel">
                    <!-- Message templates here -->
                </ItemsControl>
            </ScrollViewer>
            
            <!-- Smart Suggestions -->
            <Border Grid.Row="2"
                    Background="{DynamicResource Carbon.Brush.Background.Tertiary}"
                    BorderBrush="{DynamicResource Carbon.Brush.Border}"
                    BorderThickness="0,1,0,1"
                    Padding="20,12">
                <StackPanel>
                    <TextBlock Text="💡 Smart Suggestions:" 
                               Style="{StaticResource Carbon.TextBlock.Base}"
                               FontSize="12" FontWeight="Medium"
                               Margin="0,0,0,8"/>
                    <WrapPanel>
                        <Button Content="Explain this error message" 
                                Style="{StaticResource Carbon.Button.Secondary}"
                                FontSize="12" Padding="8,4" Margin="0,0,8,4"/>
                        <Button Content="Generate unit tests" 
                                Style="{StaticResource Carbon.Button.Secondary}"
                                FontSize="12" Padding="8,4" Margin="0,0,8,4"/>
                        <Button Content="Review code for security" 
                                Style="{StaticResource Carbon.Button.Secondary}"
                                FontSize="12" Padding="8,4" Margin="0,0,8,4"/>
                        <Button Content="Create documentation" 
                                Style="{StaticResource Carbon.Button.Secondary}"
                                FontSize="12" Padding="8,4" Margin="0,0,8,4"/>
                    </WrapPanel>
                </StackPanel>
            </Border>
            
            <!-- Enhanced Input Area -->
            <Border Grid.Row="3" Padding="20,16">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    
                    <Button Grid.Column="0" Content="📎" 
                            Style="{StaticResource Carbon.Button.IconSmall}"
                            ToolTip="Attach File" Width="36" Height="36" Margin="0,0,8,0"/>
                    <Button Grid.Column="1" Content="📋" 
                            Style="{StaticResource Carbon.Button.IconSmall}"
                            ToolTip="Use Clipboard" Width="36" Height="36" Margin="0,0,8,0"/>
                    <Button Grid.Column="2" Content="🔧" 
                            Style="{StaticResource Carbon.Button.IconSmall}"
                            ToolTip="Quick Tools" Width="36" Height="36" Margin="0,0,12,0"/>
                    
                    <TextBox Grid.Column="3" 
                             x:Name="MainMessageInput"
                             Style="{StaticResource Carbon.TextBox}"
                             FontSize="14"
                             MinHeight="36"
                             MaxHeight="120"
                             VerticalContentAlignment="Top"
                             Padding="12,8"
                             TextWrapping="Wrap"
                             AcceptsReturn="True"
                             VerticalScrollBarVisibility="Auto"/>
                    
                    <Button Grid.Column="4" Content="🎤" 
                            Style="{StaticResource Carbon.Button.IconSmall}"
                            ToolTip="Voice Input" Width="36" Height="36" Margin="12,0,8,0"/>
                    <Button Grid.Column="5" Content="📤" 
                            Style="{StaticResource Carbon.Button.Primary}"
                            ToolTip="Send Message" Width="36" Height="36"/>
                </Grid>
            </Border>
        </Grid>
        
        <!-- Right Splitter -->
        <GridSplitter Grid.Column="3" 
                      Width="5" 
                      Background="{DynamicResource Carbon.Brush.Background.Tertiary}"
                      VerticalAlignment="Stretch"/>
        
        <!-- Right Sidebar - Context Panel -->
        <Border Grid.Column="4" 
                Background="{DynamicResource Carbon.Brush.Background.Secondary}"
                BorderBrush="{DynamicResource Carbon.Brush.Border}"
                BorderThickness="1,0,0,0">
            
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>
                
                <!-- Context Header -->
                <Border Grid.Row="0" Padding="16,12">
                    <TextBlock Text="📊 Context" 
                               Style="{StaticResource Carbon.TextBlock.Base}"
                               FontWeight="SemiBold"/>
                </Border>
                
                <!-- Context Content -->
                <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto">
                    <StackPanel Margin="16,0,16,16">
                        
                        <!-- Current File -->
                        <Border Style="{StaticResource Carbon.Card.Subtle}" Margin="0,0,0,12">
                            <StackPanel>
                                <TextBlock Text="📁 Current File" 
                                           FontWeight="SemiBold" FontSize="12" 
                                           Margin="0,0,0,8"/>
                                <TextBlock Text="AuthService.cs" 
                                           FontSize="11" 
                                           Foreground="{StaticResource Carbon.Brush.Text.Secondary}"/>
                                <TextBlock Text="Lines: 247 | Size: 8.2KB" 
                                           FontSize="10" 
                                           Foreground="{StaticResource Carbon.Brush.Text.Secondary}"
                                           Margin="0,4,0,0"/>
                            </StackPanel>
                        </Border>
                        
                        <!-- Clipboard -->
                        <Border Style="{StaticResource Carbon.Card.Subtle}" Margin="0,0,0,12">
                            <StackPanel>
                                <TextBlock Text="📋 Clipboard" 
                                           FontWeight="SemiBold" FontSize="12" 
                                           Margin="0,0,0,8"/>
                                <TextBlock Text="async Task ExecuteHandler..." 
                                           FontSize="11" 
                                           Foreground="{StaticResource Carbon.Brush.Text.Secondary}"
                                           TextTrimming="CharacterEllipsis"/>
                                <TextBlock Text="85 characters" 
                                           FontSize="10" 
                                           Foreground="{StaticResource Carbon.Brush.Text.Secondary}"
                                           Margin="0,4,0,0"/>
                            </StackPanel>
                        </Border>
                        
                        <!-- Active Tools -->
                        <Border Style="{StaticResource Carbon.Card.Subtle}" Margin="0,0,0,12">
                            <StackPanel>
                                <TextBlock Text="🔧 Active Tools" 
                                           FontWeight="SemiBold" FontSize="12" 
                                           Margin="0,0,0,8"/>
                                <StackPanel>
                                    <TextBlock Text="• read_file" FontSize="11"/>
                                    <TextBlock Text="• grep" FontSize="11"/>
                                    <TextBlock Text="• web_search" FontSize="11"/>
                                </StackPanel>
                            </StackPanel>
                        </Border>
                        
                        <!-- Usage Stats -->
                        <Border Style="{StaticResource Carbon.Card.Subtle}" Margin="0,0,0,12">
                            <StackPanel>
                                <TextBlock Text="📊 Session Stats" 
                                           FontWeight="SemiBold" FontSize="12" 
                                           Margin="0,0,0,8"/>
                                <Grid>
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="Auto"/>
                                    </Grid.ColumnDefinitions>
                                    <Grid.RowDefinitions>
                                        <RowDefinition/>
                                        <RowDefinition/>
                                        <RowDefinition/>
                                        <RowDefinition/>
                                    </Grid.RowDefinitions>
                                    
                                    <TextBlock Grid.Row="0" Grid.Column="0" Text="Tokens:" FontSize="11"/>
                                    <TextBlock Grid.Row="0" Grid.Column="1" Text="247" FontSize="11" FontWeight="SemiBold"/>
                                    
                                    <TextBlock Grid.Row="1" Grid.Column="0" Text="Response:" FontSize="11"/>
                                    <TextBlock Grid.Row="1" Grid.Column="1" Text="1.2s" FontSize="11" FontWeight="SemiBold"/>
                                    
                                    <TextBlock Grid.Row="2" Grid.Column="0" Text="Cost:" FontSize="11"/>
                                    <TextBlock Grid.Row="2" Grid.Column="1" Text="$0.03" FontSize="11" FontWeight="SemiBold"/>
                                    
                                    <TextBlock Grid.Row="3" Grid.Column="0" Text="Tools:" FontSize="11"/>
                                    <TextBlock Grid.Row="3" Grid.Column="1" Text="3 calls" FontSize="11" FontWeight="SemiBold"/>
                                </Grid>
                            </StackPanel>
                        </Border>
                        
                    </StackPanel>
                </ScrollViewer>
            </Grid>
        </Border>
    </Grid>
</UserControl>
```

---

## 🔄 Interactive Components

### 🔧 **Tool Execution Indicator**

```xml
<UserControl x:Class="WpfInteractionApp.Components.ToolExecutionIndicator">
    <Border Background="{DynamicResource Carbon.Brush.Background.Tertiary}"
            BorderBrush="{DynamicResource Carbon.Brush.Border}"
            BorderThickness="1"
            CornerRadius="8"
            Padding="12,8">
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            
            <!-- Tool Icon -->
            <Border Grid.Column="0" 
                    Background="{DynamicResource Carbon.Brush.Info.Primary}"
                    CornerRadius="12"
                    Width="24" Height="24"
                    Margin="0,0,8,0">
                <TextBlock Text="🔧" 
                           HorizontalAlignment="Center" 
                           VerticalAlignment="Center"
                           FontSize="12"/>
            </Border>
            
            <!-- Tool Info -->
            <StackPanel Grid.Column="1">
                <TextBlock Text="{Binding ToolName}" 
                           FontWeight="SemiBold" FontSize="12"/>
                <TextBlock Text="{Binding Status}" 
                           FontSize="11" 
                           Foreground="{DynamicResource Carbon.Brush.Text.Secondary}"/>
            </StackPanel>
            
            <!-- Progress -->
            <StackPanel Grid.Column="2">
                <ProgressBar Width="80" Height="4" 
                             Value="{Binding Progress}"
                             Style="{StaticResource Carbon.ProgressBar}"/>
                <TextBlock Text="{Binding ProgressText}" 
                           FontSize="10" 
                           HorizontalAlignment="Right"
                           Margin="0,2,0,0"/>
            </StackPanel>
        </Grid>
    </Border>
</UserControl>
```

### 📝 **UserInputRequest Integration**

```csharp
public class AIUserInputRequest
{
    public string Title { get; set; }
    public string Message { get; set; }
    public AIInputType InputType { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    
    public enum AIInputType
    {
        Text,
        Selection,
        MultiSelection,
        FilePicker,
        ConfirmationDialog,
        SliderValue,
        ApiKeyInput
    }
}

// Usage in AI Chat
private async Task<string> RequestUserInputAsync(AIUserInputRequest request)
{
    var dialog = new UserInputDialog();
    dialog.SetRequest(request);
    
    if (dialog.ShowDialog() == true)
    {
        return dialog.GetResult();
    }
    
    return null;
}

// Example: API Key Configuration
private async Task ConfigureOpenAIAsync()
{
    var request = new AIUserInputRequest
    {
        Title = "OpenAI Configuration",
        Message = "Please enter your OpenAI API key:",
        InputType = AIInputType.ApiKeyInput,
        Parameters = new Dictionary<string, object>
        {
            ["validation_pattern"] = @"^sk-[a-zA-Z0-9]{48}$",
            ["is_required"] = true,
            ["is_password"] = true
        }
    };
    
    var apiKey = await RequestUserInputAsync(request);
    if (!string.IsNullOrEmpty(apiKey))
    {
        await SaveApiKeyAsync(apiKey);
        ShowToast("API key configured successfully!", LogType.Success);
    }
}
```

### 🔔 **Toast Notifications for AI Operations**

```csharp
public class AIToastManager
{
    public void ShowToolExecution(string toolName, string operation)
    {
        var toast = new ToastNotification
        {
            Title = $"🔧 {toolName}",
            Message = operation,
            Type = LogType.Info,
            Duration = 3,
            ShowProgress = true
        };
        
        toast.Show();
    }
    
    public void ShowToolResult(string toolName, string result, bool success)
    {
        var toast = new ToastNotification
        {
            Title = success ? $"✅ {toolName}" : $"❌ {toolName}",
            Message = result,
            Type = success ? LogType.Success : LogType.Error,
            Duration = success ? 3 : 5,
            Actions = success ? new[]
            {
                ToastActions.ViewDetails(() => ShowDetailedResult(result)),
                ToastActions.CopyToClipboard(() => CopyResult(result))
            } : new[]
            {
                ToastActions.Retry(() => RetryOperation(toolName)),
                ToastActions.ViewLogs(() => ShowErrorLogs())
            }
        };
        
        toast.Show();
    }
    
    public void ShowFileOperationConfirmation(string fileName, string operation)
    {
        var toast = new ToastNotification
        {
            Title = "⚠️ File Operation",
            Message = $"AI wants to {operation} '{fileName}'. Allow?",
            Type = LogType.Warning,
            Duration = 10,
            Actions = new[]
            {
                ToastActions.Yes(() => ConfirmFileOperation(true)),
                ToastActions.No(() => ConfirmFileOperation(false)),
                ToastActions.ViewFile(() => PreviewFile(fileName))
            }
        };
        
        toast.Show();
    }
}
```

---

## 📱 Responsive Behavior

### 🔄 **Mode Transitions**

```csharp
public class AIChatModeManager
{
    private AIChatWindow _fullWindow;
    private AIQuickChatWindow _quickWindow;
    private ChatState _currentState;
    
    public async Task SwitchToQuickModeAsync()
    {
        // Save current state
        _currentState = await _fullWindow.SaveStateAsync();
        
        // Create quick window
        _quickWindow = new AIQuickChatWindow();
        await _quickWindow.LoadStateAsync(_currentState);
        
        // Position near cursor or screen edge
        PositionQuickWindow();
        
        // Show with animation
        await AnimateTransitionAsync(_fullWindow, _quickWindow);
        
        // Hide full window
        _fullWindow.Visibility = Visibility.Hidden;
        _quickWindow.Show();
    }
    
    public async Task SwitchToFullModeAsync()
    {
        // Save quick window state
        _currentState = await _quickWindow.SaveStateAsync();
        
        // Update full window
        await _fullWindow.LoadStateAsync(_currentState);
        
        // Show full window
        _fullWindow.Visibility = Visibility.Visible;
        _fullWindow.Activate();
        
        // Close quick window with animation
        await AnimateCloseAsync(_quickWindow);
    }
    
    private void PositionQuickWindow()
    {
        var screen = System.Windows.Forms.Screen.FromPoint(
            System.Windows.Forms.Cursor.Position);
        
        // Position at bottom-right of screen
        _quickWindow.Left = screen.WorkingArea.Right - _quickWindow.Width - 20;
        _quickWindow.Top = screen.WorkingArea.Bottom - _quickWindow.Height - 20;
    }
}
```

### 📐 **Adaptive Layout**

```xml
<!-- Responsive Grid with DataTriggers -->
<Grid x:Name="ResponsiveContainer">
    <Grid.Style>
        <Style TargetType="Grid">
            <Style.Triggers>
                <!-- Mobile Layout: < 600px -->
                <DataTrigger Binding="{Binding RelativeSource={RelativeSource AncestorType=Window}, 
                                     Path=ActualWidth, 
                                     Converter={StaticResource WidthToLayoutConverter}}" 
                             Value="Mobile">
                    <Setter Property="Margin" Value="8"/>
                </DataTrigger>
                
                <!-- Tablet Layout: 600-1000px -->
                <DataTrigger Binding="{Binding RelativeSource={RelativeSource AncestorType=Window}, 
                                     Path=ActualWidth, 
                                     Converter={StaticResource WidthToLayoutConverter}}" 
                             Value="Tablet">
                    <Setter Property="Margin" Value="16"/>
                </DataTrigger>
                
                <!-- Desktop Layout: > 1000px -->
                <DataTrigger Binding="{Binding RelativeSource={RelativeSource AncestorType=Window}, 
                                     Path=ActualWidth, 
                                     Converter={StaticResource WidthToLayoutConverter}}" 
                             Value="Desktop">
                    <Setter Property="Margin" Value="24"/>
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Grid.Style>
    
    <!-- Responsive column definitions -->
    <Grid.ColumnDefinitions>
        <!-- Sidebar - Hidden on mobile -->
        <ColumnDefinition>
            <ColumnDefinition.Style>
                <Style TargetType="ColumnDefinition">
                    <Setter Property="Width" Value="250"/>
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding LayoutMode}" Value="Mobile">
                            <Setter Property="Width" Value="0"/>
                        </DataTrigger>
                        <DataTrigger Binding="{Binding LayoutMode}" Value="Tablet">
                            <Setter Property="Width" Value="200"/>
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </ColumnDefinition.Style>
        </ColumnDefinition>
        
        <!-- Main content -->
        <ColumnDefinition Width="*"/>
        
        <!-- Context panel - Hidden on small screens -->
        <ColumnDefinition>
            <ColumnDefinition.Style>
                <Style TargetType="ColumnDefinition">
                    <Setter Property="Width" Value="200"/>
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding LayoutMode}" Value="Mobile">
                            <Setter Property="Width" Value="0"/>
                        </DataTrigger>
                        <DataTrigger Binding="{Binding LayoutMode}" Value="Tablet">
                            <Setter Property="Width" Value="0"/>
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </ColumnDefinition.Style>
        </ColumnDefinition>
    </Grid.ColumnDefinitions>
</Grid>
```

---

## 🎭 User Interaction Patterns

### 💬 **Conversational Flow Examples**

#### **Scenario 1: First Time Setup**
```
🤖 Welcome to AI Assistant! I'm here to help you with your development tasks.

   Let me help you get started. I'll need to configure a few things:

   1️⃣ **AI Provider**: Which AI service would you like to use?
   
   [🤖 OpenAI GPT-4] [🧠 Claude] [💎 Gemini] [🏠 Local LLM]
   
   2️⃣ **Tools**: Which tools should I enable for you?
   
   ✅ Code Search    ✅ File Reader    ❌ File Editor (Sandbox required)
   ✅ Web Search     ❌ Terminal       ❌ Email Sender
   
   3️⃣ **Notifications**: How should I notify you?
   
   [🔔 Toast Notifications] [📧 Email] [💬 Chat Only]
   
   Ready to continue? [▶️ Next Step]
```

#### **Scenario 2: Error Recovery**
```
🤖 I encountered an issue while trying to read the file:

   ❌ **Error**: Access denied to 'secure_config.json'
   
   🛠️ **Possible Solutions**:
   1. Grant file permissions in sandbox settings
   2. Move file to allowed directory
   3. Use a different file
   
   📋 **Quick Actions**:
   [⚙️ Open Sandbox Settings] [📁 Browse Files] [🔄 Retry]
   
   Would you like me to try a different approach?
```

#### **Scenario 3: Proactive Assistance**
```
🤖 I noticed you're working on authentication code. 

   💡 **Smart Suggestions**:
   • I can review this code for security best practices
   • Generate unit tests for the authentication methods  
   • Check for common auth vulnerabilities
   • Create API documentation
   
   What would be most helpful right now?
   
   [🔍 Security Review] [🧪 Generate Tests] [📚 Create Docs] [❌ Not Now]
```

### 🎯 **Input Validation Patterns**

```csharp
public class AIInputValidator
{
    public ValidationResult ValidateMessage(string message)
    {
        var result = new ValidationResult();
        
        // Length validation
        if (string.IsNullOrWhiteSpace(message))
        {
            result.IsValid = false;
            result.ErrorMessage = "Message cannot be empty";
            return result;
        }
        
        if (message.Length > 4000)
        {
            result.IsValid = false;
            result.ErrorMessage = "Message too long (max 4000 characters)";
            return result;
        }
        
        // Content safety check
        if (ContainsSensitiveContent(message))
        {
            result.IsValid = false;
            result.ErrorMessage = "Message contains sensitive information";
            result.Suggestions = new[]
            {
                "Remove personal data before sending",
                "Use placeholder values for sensitive info",
                "Enable privacy mode"
            };
            return result;
        }
        
        // Cost estimation
        result.EstimatedTokens = EstimateTokens(message);
        result.EstimatedCost = CalculateCost(result.EstimatedTokens);
        
        if (result.EstimatedCost > 1.0m) // $1 threshold
        {
            result.RequiresConfirmation = true;
            result.WarningMessage = $"This message may cost ${result.EstimatedCost:F2}. Continue?";
        }
        
        result.IsValid = true;
        return result;
    }
}
```

### 🔐 **Permission Request Patterns**

```csharp
public class AIPermissionManager
{
    public async Task<bool> RequestFileAccessAsync(string filePath, FileOperation operation)
    {
        var request = new UserInputRequest
        {
            Title = "🔐 File Access Permission",
            Message = $"AI Assistant wants to {operation.ToString().ToLower()} the file:\n\n'{filePath}'\n\nAllow this operation?",
            IsRequired = true,
            IsSelectionList = true,
            SelectionItems = new[]
            {
                new SelectionItem { Value = "allow_once", Display = "✅ Allow Once" },
                new SelectionItem { Value = "allow_always", Display = "✅ Always Allow for this file" },
                new SelectionItem { Value = "deny", Display = "❌ Deny" },
                new SelectionItem { Value = "preview", Display = "👁️ Preview Changes First" }
            }
        };
        
        var result = await ShowUserInputDialogAsync(request);
        
        switch (result)
        {
            case "allow_once":
                return true;
            case "allow_always":
                AddToTrustedFiles(filePath);
                return true;
            case "preview":
                await ShowFilePreviewAsync(filePath, operation);
                return await RequestFileAccessAsync(filePath, operation); // Recursive call
            case "deny":
            default:
                return false;
        }
    }
}
```

---

## 🧩 Component Architecture

### 🔧 **Reusable Components**

#### **ChatBubble Component**
```xml
<UserControl x:Class="WpfInteractionApp.Components.ChatBubble">
    <UserControl.Resources>
        <Storyboard x:Key="FadeInAnimation">
            <DoubleAnimation Storyboard.TargetProperty="Opacity"
                           From="0" To="1" Duration="0:0:0.3"/>
            <ThicknessAnimation Storyboard.TargetProperty="Margin"
                              From="0,20,0,0" To="0,0,0,0" Duration="0:0:0.3"/>
        </Storyboard>
    </UserControl.Resources>
    
    <Border x:Name="BubbleBorder" 
            MaxWidth="400"
            Margin="0,8"
            Padding="12,8"
            CornerRadius="12,12,4,12"
            Background="{Binding Role, Converter={StaticResource RoleToBrushConverter}}">
        <Border.Triggers>
            <EventTrigger RoutedEvent="Loaded">
                <BeginStoryboard Storyboard="{StaticResource FadeInAnimation}"/>
            </EventTrigger>
        </Border.Triggers>
        
        <StackPanel>
            <!-- Message Content -->
            <RichTextBox x:Name="MessageContent"
                         Background="Transparent"
                         BorderThickness="0"
                         IsReadOnly="True"
                         FontSize="13">
                <!-- Rich content with markdown support -->
            </RichTextBox>
            
            <!-- Tool Execution Indicators -->
            <ItemsControl ItemsSource="{Binding ToolExecutions}"
                          Margin="0,8,0,0">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <local:ToolExecutionIndicator/>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
            
            <!-- Action Buttons -->
            <StackPanel Orientation="Horizontal" 
                        HorizontalAlignment="Right"
                        Margin="0,8,0,0">
                <Button Content="📋" Style="{StaticResource Carbon.Button.IconSmall}"
                        ToolTip="Copy" Width="24" Height="24" Margin="2"/>
                <Button Content="🔄" Style="{StaticResource Carbon.Button.IconSmall}"
                        ToolTip="Regenerate" Width="24" Height="24" Margin="2"/>
                <Button Content="👍" Style="{StaticResource Carbon.Button.IconSmall}"
                        ToolTip="Good Response" Width="24" Height="24" Margin="2"/>
                <Button Content="👎" Style="{StaticResource Carbon.Button.IconSmall}"
                        ToolTip="Poor Response" Width="24" Height="24" Margin="2"/>
            </StackPanel>
            
            <!-- Timestamp -->
            <TextBlock Text="{Binding Timestamp, StringFormat=HH:mm}"
                       FontSize="10"
                       Foreground="{StaticResource Carbon.Brush.Text.Secondary}"
                       HorizontalAlignment="Right"
                       Margin="0,4,0,0"/>
        </StackPanel>
    </Border>
</UserControl>
```

#### **SmartInputBox Component**
```xml
<UserControl x:Class="WpfInteractionApp.Components.SmartInputBox">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Input Area -->
        <Border Grid.Row="1" 
                Background="{DynamicResource Carbon.Brush.Background.Secondary}"
                BorderBrush="{DynamicResource Carbon.Brush.Border}"
                BorderThickness="1"
                CornerRadius="8">
            <Grid>
                <TextBox x:Name="MessageInput"
                         Style="{StaticResource Carbon.TextBox}"
                         BorderThickness="0"
                         Background="Transparent"
                         FontSize="14"
                         MinHeight="40"
                         MaxHeight="120"
                         Padding="12,8"
                         TextWrapping="Wrap"
                         AcceptsReturn="True"
                         VerticalScrollBarVisibility="Auto"
                         TextChanged="MessageInput_TextChanged"/>
                
                <!-- Placeholder Text -->
                <TextBlock Text="Type your message... (Ctrl+Enter to send)"
                           Foreground="#999999"
                           FontSize="14"
                           Margin="12,8"
                           VerticalAlignment="Top"
                           IsHitTestVisible="False">
                    <TextBlock.Style>
                        <Style TargetType="TextBlock">
                            <Setter Property="Visibility" Value="Collapsed"/>
                            <Style.Triggers>
                                <DataTrigger Binding="{Binding Text, ElementName=MessageInput}" Value="">
                                    <Setter Property="Visibility" Value="Visible"/>
                                </DataTrigger>
                            </Style.Triggers>
                        </Style>
                    </TextBlock.Style>
                </TextBlock>
                
                <!-- Typing Indicator -->
                <Border x:Name="TypingIndicator"
                        Background="{StaticResource Carbon.Brush.Info.Primary}"
                        CornerRadius="12"
                        Padding="8,4"
                        HorizontalAlignment="Right"
                        VerticalAlignment="Bottom"
                        Margin="8"
                        Visibility="Collapsed">
                    <StackPanel Orientation="Horizontal">
                        <TextBlock Text="AI is typing" Foreground="White" FontSize="11"/>
                        <StackPanel Orientation="Horizontal" Margin="8,0,0,0">
                            <Ellipse Width="4" Height="4" Fill="White" Margin="1">
                                <Ellipse.Triggers>
                                    <EventTrigger RoutedEvent="Loaded">
                                        <BeginStoryboard RepeatBehavior="Forever">
                                            <Storyboard>
                                                <DoubleAnimation Storyboard.TargetProperty="Opacity"
                                                               From="0.3" To="1" Duration="0:0:0.6"
                                                               AutoReverse="True"/>
                                            </Storyboard>
                                        </BeginStoryboard>
                                    </EventTrigger>
                                </Ellipse.Triggers>
                            </Ellipse>
                            <Ellipse Width="4" Height="4" Fill="White" Margin="1">
                                <Ellipse.Triggers>
                                    <EventTrigger RoutedEvent="Loaded">
                                        <BeginStoryboard RepeatBehavior="Forever">
                                            <Storyboard BeginTime="0:0:0.2">
                                                <DoubleAnimation Storyboard.TargetProperty="Opacity"
                                                               From="0.3" To="1" Duration="0:0:0.6"
                                                               AutoReverse="True"/>
                                            </Storyboard>
                                        </BeginStoryboard>
                                    </EventTrigger>
                                </Ellipse.Triggers>
                            </Ellipse>
                            <Ellipse Width="4" Height="4" Fill="White" Margin="1">
                                <Ellipse.Triggers>
                                    <EventTrigger RoutedEvent="Loaded">
                                        <BeginStoryboard RepeatBehavior="Forever">
                                            <Storyboard BeginTime="0:0:0.4">
                                                <DoubleAnimation Storyboard.TargetProperty="Opacity"
                                                               From="0.3" To="1" Duration="0:0:0.6"
                                                               AutoReverse="True"/>
                                            </Storyboard>
                                        </BeginStoryboard>
                                    </EventTrigger>
                                </Ellipse.Triggers>
                            </Ellipse>
                        </StackPanel>
                    </StackPanel>
                </Border>
            </Grid>
        </Border>
        
        <!-- Action Bar -->
        <StackPanel Grid.Row="2" 
                    Orientation="Horizontal" 
                    HorizontalAlignment="Right"
                    Margin="0,8,0,0">
            
            <!-- Character Count -->
            <TextBlock x:Name="CharacterCount"
                       Text="0 / 4000"
                       FontSize="11"
                       Foreground="{StaticResource Carbon.Brush.Text.Secondary}"
                       VerticalAlignment="Center"
                       Margin="0,0,12,0"/>
            
            <!-- Cost Estimate -->
            <TextBlock x:Name="CostEstimate"
                       Text="~$0.001"
                       FontSize="11"
                       Foreground="{StaticResource Carbon.Brush.Text.Secondary}"
                       VerticalAlignment="Center"
                       Margin="0,0,12,0"/>
            
            <!-- Action Buttons -->
            <Button Content="📎" Style="{StaticResource Carbon.Button.IconSmall}"
                    ToolTip="Attach File" Width="32" Height="32" Margin="2"/>
            <Button Content="📋" Style="{StaticResource Carbon.Button.IconSmall}"
                    ToolTip="Use Clipboard" Width="32" Height="32" Margin="2"/>
            <Button Content="🎤" Style="{StaticResource Carbon.Button.IconSmall}"
                    ToolTip="Voice Input" Width="32" Height="32" Margin="2"/>
            <Button Content="📤" Style="{StaticResource Carbon.Button.Primary}"
                    ToolTip="Send (Ctrl+Enter)" Width="32" Height="32" Margin="8,2,0,2"/>
        </StackPanel>
    </Grid>
</UserControl>
```

---

## 🎨 Theme Integration

### 🌈 **Dynamic Theme Support**

```csharp
public class AIThemeManager : IThemeAware
{
    public void OnThemeChanged(string theme)
    {
        // Update chat bubble colors
        UpdateChatBubbleTheme(theme);
        
        // Update syntax highlighting
        UpdateSyntaxHighlighting(theme);
        
        // Update tool execution indicators
        UpdateToolIndicatorTheme(theme);
        
        // Animate theme transition
        AnimateThemeTransition(theme);
    }
    
    private void UpdateChatBubbleTheme(string theme)
    {
        var userBubbleColor = theme switch
        {
            "Dark" => "#4589FF",
            "Light" => "#0F62FE", 
            "Dim" => "#4589FF",
            _ => "#4589FF"
        };
        
        var assistantBubbleColor = theme switch
        {
            "Dark" => "#262626",
            "Light" => "#F4F4F4",
            "Dim" => "#393939",
            _ => "#262626"
        };
        
        // Apply colors to chat bubbles
        ApplyBubbleColors(userBubbleColor, assistantBubbleColor);
    }
    
    private void AnimateThemeTransition(string theme)
    {
        var storyboard = new Storyboard();
        
        // Fade out
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));
        storyboard.Children.Add(fadeOut);
        
        // Apply new theme
        storyboard.Completed += (s, e) =>
        {
            ApplyTheme(theme);
            
            // Fade in
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
            var fadeInStoryboard = new Storyboard();
            fadeInStoryboard.Children.Add(fadeIn);
            fadeInStoryboard.Begin();
        };
        
        storyboard.Begin();
    }
}
```

### 🎭 **Custom Theme Variables**

```xml
<!-- AI Chat Specific Theme Resources -->
<ResourceDictionary>
    <!-- Chat Bubble Colors -->
    <SolidColorBrush x:Key="AI.Brush.ChatBubble.User" Color="#4589FF"/>
    <SolidColorBrush x:Key="AI.Brush.ChatBubble.Assistant" Color="#262626"/>
    <SolidColorBrush x:Key="AI.Brush.ChatBubble.System" Color="#FF6B6B"/>
    <SolidColorBrush x:Key="AI.Brush.ChatBubble.Error" Color="#FA4D56"/>
    
    <!-- Tool Execution Colors -->
    <SolidColorBrush x:Key="AI.Brush.Tool.Executing" Color="#4589FF"/>
    <SolidColorBrush x:Key="AI.Brush.Tool.Success" Color="#42BE65"/>
    <SolidColorBrush x:Key="AI.Brush.Tool.Error" Color="#FA4D56"/>
    <SolidColorBrush x:Key="AI.Brush.Tool.Warning" Color="#F1C21B"/>
    
    <!-- Context Panel Colors -->
    <SolidColorBrush x:Key="AI.Brush.Context.Current" Color="#4589FF"/>
    <SolidColorBrush x:Key="AI.Brush.Context.Clipboard" Color="#8A3FFC"/>
    <SolidColorBrush x:Key="AI.Brush.Context.Tools" Color="#FF6B6B"/>
    
    <!-- Animation Durations -->
    <Duration x:Key="AI.Animation.Fast">0:0:0.2</Duration>
    <Duration x:Key="AI.Animation.Medium">0:0:0.3</Duration>
    <Duration x:Key="AI.Animation.Slow">0:0:0.5</Duration>
    
    <!-- Shadow Effects -->
    <DropShadowEffect x:Key="AI.Shadow.ChatBubble" 
                      ShadowDepth="2" 
                      Direction="270" 
                      Color="#40000000" 
                      BlurRadius="8" 
                      Opacity="0.2"/>
</ResourceDictionary>
```

---

## ⚡ Performance Optimizations

### 🔄 **Virtual Scrolling for Large Conversations**

```csharp
public class VirtualizedChatPanel : VirtualizingPanel
{
    private readonly Dictionary<int, UIElement> _realizedItems = new();
    private ScrollViewer _scrollViewer;
    
    protected override Size MeasureOverride(Size availableSize)
    {
        var itemHeight = 60; // Average chat bubble height
        var totalItems = InternalChildren.Count;
        var totalHeight = totalItems * itemHeight;
        
        var viewport = GetViewportInfo();
        var firstVisible = Math.Max(0, (int)(viewport.Offset / itemHeight) - 2);
        var lastVisible = Math.Min(totalItems - 1, 
            (int)((viewport.Offset + viewport.ViewportHeight) / itemHeight) + 2);
        
        // Virtualize: only measure visible items
        for (int i = firstVisible; i <= lastVisible; i++)
        {
            if (!_realizedItems.ContainsKey(i))
            {
                var item = CreateChatBubble(i);
                _realizedItems[i] = item;
                InternalChildren.Add(item);
            }
            
            _realizedItems[i].Measure(availableSize);
        }
        
        // Remove items outside viewport
        var itemsToRemove = _realizedItems.Keys
            .Where(i => i < firstVisible || i > lastVisible)
            .ToList();
            
        foreach (var index in itemsToRemove)
        {
            InternalChildren.Remove(_realizedItems[index]);
            _realizedItems.Remove(index);
        }
        
        return new Size(availableSize.Width, totalHeight);
    }
    
    private UIElement CreateChatBubble(int index)
    {
        var message = GetMessage(index);
        var bubble = new ChatBubble();
        bubble.DataContext = message;
        return bubble;
    }
}
```

### 🎭 **Lazy Loading for Media Content**

```csharp
public class LazyImageLoader
{
    private readonly Dictionary<string, BitmapImage> _imageCache = new();
    private readonly SemaphoreSlim _loadingSemaphore = new(3); // Max 3 concurrent loads
    
    public async Task<BitmapImage> LoadImageAsync(string url)
    {
        if (_imageCache.TryGetValue(url, out var cachedImage))
        {
            return cachedImage;
        }
        
        await _loadingSemaphore.WaitAsync();
        
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(url);
            image.DecodePixelWidth = 400; // Optimize size
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze(); // Make thread-safe
            
            _imageCache[url] = image;
            return image;
        }
        finally
        {
            _loadingSemaphore.Release();
        }
    }
}
```

### 📊 **Memory Management**

```csharp
public class ChatMemoryManager
{
    private const int MAX_MESSAGES = 1000;
    private const int CLEANUP_THRESHOLD = 1200;
    
    public void OptimizeMemory(ObservableCollection<ChatMessage> messages)
    {
        if (messages.Count > CLEANUP_THRESHOLD)
        {
            // Keep recent messages and important ones
            var messagesToKeep = messages
                .Take(50) // Recent 50 messages
                .Union(messages.Where(m => m.IsImportant)) // Important messages
                .Union(messages.TakeLast(50)) // Last 50 messages
                .Distinct()
                .OrderBy(m => m.Timestamp)
                .ToList();
            
            messages.Clear();
            foreach (var message in messagesToKeep)
            {
                messages.Add(message);
            }
            
            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
    
    public void CompressOldMessages(ChatMessage message)
    {
        if (message.Age > TimeSpan.FromDays(7))
        {
            // Compress content for old messages
            message.Content = CompressText(message.Content);
            message.IsCompressed = true;
        }
    }
}
```

---

## 🧪 Interactive Prototypes

### 🎮 **Mock Data for Testing**

```csharp
public class MockAIService
{
    private readonly Random _random = new();
    
    public async Task<string> GenerateMockResponseAsync(string userMessage)
    {
        // Simulate thinking delay
        await Task.Delay(_random.Next(500, 2000));
        
        var responses = new[]
        {
            "I'll help you with that! Let me analyze the code...",
            "That's an interesting question. Based on the context, I can suggest...",
            "I found several optimization opportunities in your code...",
            "Let me search through the codebase to find relevant examples...",
            "I notice you're working on authentication. Here are some security best practices..."
        };
        
        return responses[_random.Next(responses.Length)];
    }
    
    public async IAsyncEnumerable<string> StreamMockResponseAsync(string userMessage)
    {
        var words = (await GenerateMockResponseAsync(userMessage)).Split(' ');
        
        foreach (var word in words)
        {
            await Task.Delay(_random.Next(50, 200));
            yield return word + " ";
        }
    }
    
    public List<ChatMessage> GenerateMockConversation()
    {
        return new List<ChatMessage>
        {
            new()
            {
                Role = "user",
                Content = "Can you help me optimize this AuthService code?",
                Timestamp = DateTime.Now.AddMinutes(-10)
            },
            new()
            {
                Role = "assistant", 
                Content = "I'll analyze the AuthService code for optimization opportunities. Let me examine it...",
                Timestamp = DateTime.Now.AddMinutes(-9),
                ToolExecutions = new[]
                {
                    new ToolExecution { Name = "read_file", Status = "Reading AuthService.cs...", Progress = 100 }
                }
            },
            new()
            {
                Role = "assistant",
                Content = "✅ Analysis complete! I found several areas for improvement:\n\n1. **Async Optimization**: The method currently blocks on database calls\n2. **Connection Pooling**: Multiple DB connections created unnecessarily\n3. **Caching**: User permissions could be cached for 5 minutes",
                Timestamp = DateTime.Now.AddMinutes(-8),
                Actions = new[]
                {
                    new MessageAction { Text = "🚀 Apply Fixes", Type = "primary" },
                    new MessageAction { Text = "📊 Benchmark", Type = "secondary" },
                    new MessageAction { Text = "📝 Create Task", Type = "secondary" }
                }
            }
        };
    }
}
```

### 🎯 **User Testing Scenarios**

```markdown
## 🧪 User Testing Checklist

### ✅ First-Time User Experience
- [ ] **Welcome Flow**: User can complete setup in < 2 minutes
- [ ] **Tool Discovery**: User understands available tools
- [ ] **First Message**: User successfully sends first message
- [ ] **Permission Setup**: User grants necessary permissions
- [ ] **Theme Selection**: User can change theme

### ✅ Daily Usage Scenarios  
- [ ] **Quick Chat**: User opens quick chat with hotkey
- [ ] **Context Awareness**: AI recognizes current file/clipboard
- [ ] **Tool Execution**: User sees tool progress and results
- [ ] **Mode Switching**: User switches between quick/full mode
- [ ] **Multi-session**: User manages multiple chat sessions

### ✅ Error Handling
- [ ] **Network Error**: Graceful handling of connection issues
- [ ] **Permission Denied**: Clear error message and recovery
- [ ] **Invalid Input**: Helpful validation messages
- [ ] **Tool Failure**: Appropriate error feedback
- [ ] **Rate Limiting**: User informed of usage limits

### ✅ Accessibility
- [ ] **Keyboard Navigation**: All features accessible via keyboard
- [ ] **Screen Reader**: Content properly announced
- [ ] **High Contrast**: UI readable in high contrast mode
- [ ] **Font Scaling**: UI scales with system font size
- [ ] **Focus Indicators**: Clear focus visualization

### ✅ Performance
- [ ] **Response Time**: < 2 seconds for UI interactions
- [ ] **Memory Usage**: < 200MB for normal usage
- [ ] **Large Conversations**: Smooth scrolling with 1000+ messages
- [ ] **File Operations**: Progress indicators for long operations
- [ ] **Theme Switching**: Smooth transitions without flicker
```

---

## 🎉 Sonuç

Bu UI/UX experiments dokümanı, AI Chat sistemi için comprehensive bir tasarım ve implementation guide sunuyor. Ana özellikler:

### 🎯 **Temel Başarılar:**
- **🔄 Dual Mode**: Quick popup + Full tab modes
- **📱 Responsive**: Mobile-first adaptive design
- **🎨 Theme Integration**: Carbon Design System uyumlu
- **⚡ Performance**: Virtual scrolling, lazy loading
- **🔧 Component Based**: Reusable, modular architecture

### 🛠️ **Implementation Ready:**
- **XAML Templates**: Production-ready code snippets
- **Integration Patterns**: UserInputRequest, Toast usage
- **Mock Data**: Testing ve prototype için
- **Performance Optimizations**: Memory management, virtualization

### 🎭 **User Experience:**
- **Intuitive Flows**: Natural conversation patterns
- **Error Recovery**: Graceful error handling
- **Accessibility**: Full keyboard navigation, screen reader support
- **Personalization**: Adaptive suggestions, customizable interface

Bu tasarımları temel alarak AI Chat sisteminizi implementasyon aşamasına geçirebilirsiniz! 🚀

Hangi component'ten başlamak istersiniz? Quick Chat popup mu yoksa Full Chat tab mı? 🤔

<function_calls>
<invoke name="todo_write">
<parameter name="merge">true
