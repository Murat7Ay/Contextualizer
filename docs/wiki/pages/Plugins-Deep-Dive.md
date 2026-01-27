# Plugins & Extensions (Deep Dive)

This page covers action, validator, and context provider plugins.

## Validators
- JSON validator: [Contextualizer.Core/Actions/JsonContentValidator.cs](Contextualizer.Core/Actions/JsonContentValidator.cs)
- XML validator: [Contextualizer.Core/Actions/XmlContentValidator.cs](Contextualizer.Core/Actions/XmlContentValidator.cs)

## Context Providers
- JSON context provider: [Contextualizer.Core/Actions/JsonContextProvider.cs](Contextualizer.Core/Actions/JsonContextProvider.cs)
- XML context provider: [Contextualizer.Core/Actions/XmlContextProvider.cs](Contextualizer.Core/Actions/XmlContextProvider.cs)

## Notes
- Validators gate handler execution.
- Context providers shape `_formatted_output` and `_input`.