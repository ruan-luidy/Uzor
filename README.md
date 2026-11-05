# Uzor

Kit de interface WPF dos meus apps (Provodnik, Elektrozavodsk, Windfinder, Handoff e, em breve, o Negativo). Uma
fonte só pros estilos, controles e comportamentos que antes viviam copiados em cada app: corrige aqui, sobe a versão,
todo mundo recebe.

O visual é o "jeito da Связь": painel lateral direto no Mica, a tela numa camada translúcida por cima, cartões mais
uma camada, nada de borda dura; o que flutua (menu, calendário, diálogo, tooltip) no cinza do menu do Windows 11.

## O que tem

- **Estilos** (`Themes/`): tokens (espaço, raio, fonte), botões por papel (`Action.*`), campos (`Field`, `FilterBox`,
  `SearchBox`), tabelas (`StandardDataGrid`, `EmbeddedDataGrid`), caixa de marcar, chave (`Switch`), escolha de opções
  (`ChoiceGroup`), abas (`SectionTab`), chips de tom, faixas de aviso, cartões de informação e de configuração, painel
  lateral (`SideNavItem`), tooltip e os seletores do HandyControl no fundo translúcido.
- **Controles** (`Uzor.Wpf.Controls`): `DialogFrame`, `AlertDialog`, `FormDialog`, `SelectField`, `DateTimeField`,
  `TimeEditor`, `Toast`, `EmptyState`, `ErrorState`, `LoadingDots`, `LiveDot`, `PageHeader`, `Chip`, `NavBadge`,
  `TipText` e o `PageHost` (as telas montam uma vez e só trocam de visibilidade, sem piscar).
- **Comportamentos** (`Uzor.Wpf.Behaviors`): `CornerClip`, `ScrollFade`, `QuickFilter`, `DataGridExport`,
  `ColumnWidthsFix`.
- **Animação** (`Uzor.Wpf.Animation`): `Motion` (cascata, tremida, pulo, deslize) e `ThemeRipple`, a troca de tema
  numa onda que sai do clique.
- **Serviços** (`Uzor.Wpf.Services`): `ThemeService` (claro e escuro de verdade, todas as camadas),
  `DialogService` e `ToastService`.

## Como usar

```xml
<!-- App.xaml: o HandyControl primeiro (ele escolhe o destaque), a Uzor em ordem, os estilos do app por último -->
<ResourceDictionary.MergedDictionaries>
  <hc:ThemeResources AccentColor="#3778bf" RequestedTheme="Dark" />
  <hc:Theme />
  <ResourceDictionary Source="pack://application:,,,/Uzor.Wpf;component/Themes/Tokens.xaml" />
  <ResourceDictionary Source="pack://application:,,,/Uzor.Wpf;component/Themes/Buttons.xaml" />
  <ResourceDictionary Source="pack://application:,,,/Uzor.Wpf;component/Themes/DesignSystem.xaml" />
  <ResourceDictionary Source="pack://application:,,,/Uzor.Wpf;component/Themes/Components.xaml" />
  <ResourceDictionary Source="pack://application:,,,/Uzor.Wpf;component/Themes/StatusColors.xaml" />
  <ResourceDictionary Source="pack://application:,,,/Uzor.Wpf;component/Themes/DataGrid.xaml" />
  <ResourceDictionary Source="pack://application:,,,/Uzor.Wpf;component/Themes/CheckBoxes.xaml" />
  <ResourceDictionary Source="pack://application:,,,/Uzor.Wpf;component/Themes/Dialogs.xaml" />
  <ResourceDictionary Source="pack://application:,,,/Uzor.Wpf;component/Themes/Shell.xaml" />
  <ResourceDictionary Source="pack://application:,,,/Uzor.Wpf;component/Themes/Kit.xaml" />
  <!-- os estilos do próprio app aqui -->
</ResourceDictionary.MergedDictionaries>
```

Um por linha e nessa ordem, direto no `App.xaml`: o `StaticResource` de um dicionário só enxerga os que já entraram no
Application. Agrupados num dicionário só, o WPF não acha o estilo de um irmão (o `Field` não acha o `FieldBox`) e a
tela quebra ao abrir.

```csharp
// Na partida, antes de abrir janela
Texts.Close = "Fechar";          // textos da biblioteca, se o app não for em português
ThemeService.Apply(dark: true);  // o tema guardado pelo app
// Quando a pessoa troca: a onda sai do mouse
ThemeService.Switch(dark: false);
```

A janela principal marca `hc:Dialog.Token="{x:Static services:DialogService.Token}"` pra receber os diálogos.

## Vitrine

`samples/Uzor.Gallery` mostra tudo, no claro e no escuro. Pra revisar sem abrir nada na tela:

```
Uzor.Gallery.exe --snapshots pasta
```

fotografa cada página nos dois temas, os diálogos, os tooltips e a onda do tema.

## Versão

Pacote `Uzor.Wpf` no GitHub Packages. Tag `vX.Y.Z` no `main` publica a versão (ver `.github/workflows`).
