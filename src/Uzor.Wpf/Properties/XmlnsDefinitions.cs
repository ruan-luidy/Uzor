using System.Windows.Markup;

// Um prefixo só no XAML de quem usa a Uzor: xmlns:uzor="https://github.com/ruan-luidy/Uzor" acha os controles, os
// comportamentos, os conversores, os ícones e as animações (05/11/2026, a migração do Negativo). O clr-namespace de
// sempre continua valendo.
[assembly: XmlnsDefinition("https://github.com/ruan-luidy/Uzor", "Uzor.Wpf")]
[assembly: XmlnsDefinition("https://github.com/ruan-luidy/Uzor", "Uzor.Wpf.Controls")]
[assembly: XmlnsDefinition("https://github.com/ruan-luidy/Uzor", "Uzor.Wpf.Behaviors")]
[assembly: XmlnsDefinition("https://github.com/ruan-luidy/Uzor", "Uzor.Wpf.Converters")]
[assembly: XmlnsDefinition("https://github.com/ruan-luidy/Uzor", "Uzor.Wpf.Icons")]
[assembly: XmlnsDefinition("https://github.com/ruan-luidy/Uzor", "Uzor.Wpf.Animation")]
[assembly: XmlnsPrefix("https://github.com/ruan-luidy/Uzor", "uzor")]
