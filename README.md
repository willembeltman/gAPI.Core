# gAPI.Core

`gAPI.Core` is het minimale kernpakket van het gAPI-ecosysteem.

Het bevat **attributen, contracten en helpers** die gebruikt worden door:
- gAPI code generators
- gegenereerde server- en clientcode
- handgeschreven services die onderdeel zijn van het gAPI model

Dit pakket bevat **geen runtime-logica**, **geen infrastructuur** en **geen code generation**.
Het is bedoeld als stabiele, lichtgewicht afhankelijkheid voor meerdere projecten.

---

## ✨ Doel

Het doel van `gAPI.Core` is om:

- Een **gedeeld semantisch model** te bieden (via attributen)
- Compile-time metadata beschikbaar te maken voor code generators
- Afhankelijkheden tussen projecten **los** en **expliciet** te houden
- Code generation mogelijk te maken zonder zware runtime coupling

---

## 📦 Inhoud

Typische onderdelen in dit pakket zijn:

- Attributen (bijv. service-, model- of property-annotaties)
- Marker interfaces
- Kleine helper utilities
- Shared enums / metadata types

> ⚠️ Belangrijk:  
> Dit pakket bevat **bewust zo min mogelijk code**.  
> Als iets runtime-logica of infrastructuur vereist, hoort het **niet** in gAPI.Core.

---

## 🔧 Gebruik

Installeer via NuGet:

```bash
dotnet add package gAPI.Core
```bash

## gAPI.CoreGebruik het pakket in je datamodels en services:

	using gAPI.Core;

	[IsService]
	public class UserService
	{
		[IsForeignKey(typeof(User))]
		public Guid UserId { get; set; }
	}


De daadwerkelijke betekenis van deze attributen wordt geïnterpreteerd door:

gAPI code generators

tooling in het consumerende project

## 🧱 Architectuur

gAPI.Core staat onderaan de gAPI stack:

	┌─────────────────────────────┐
	│ Generated Clients / UI      │
	├─────────────────────────────┤
	│ Generated Server / API      │
	├─────────────────────────────┤
	│ gAPI Code Generators        │
	├─────────────────────────────┤
	│ gAPI.Core                   │  ← dit pakket
	└─────────────────────────────┘


Hierdoor kan:

één core package gebruikt worden door meerdere oplossingen

code generation per project evolueren zonder breaking changes in core

tooling onafhankelijk getest en ontwikkeld worden

## 🔓 Licentie

MIT License
Zie LICENSE voor details.

## 🚧 Status

Dit project is actief in ontwikkeling, maar wordt bewust stabiel gehouden.
Breaking changes worden zoveel mogelijk vermeden, omdat dit pakket breed gebruikt wordt
binnen het gAPI-ecosysteem.

## 💬 Context

gAPI is ontstaan vanuit de behoefte aan:

- attribuut-gedreven ontwikkeling
- consistente API- en clientgeneratie
- minimale handgeschreven boilerplate
- maximale controle over gegenereerde code
- gAPI.Core is de fundering waarop dit alles rust.