using System.Globalization;
using JsonToCvApi.Models;

namespace JsonToCvApi.Localization;

/// <summary>Hardcoded label/culture lookup behind the `language` argument on both entry points.</summary>
public sealed class CvLocalizationProvider : ICvLocalizationProvider
{
    public CvLabels GetLabels(CvLanguage language) => LabelsByLanguage[language];

    public CultureInfo GetCulture(CvLanguage language) => CultureByLanguage[language];

    private static readonly Dictionary<CvLanguage, CvLabels> LabelsByLanguage = new()
    {
        [CvLanguage.En] = new(
            Summary: "Summary", Skills: "Skills", Experience: "Experience", Education: "Education",
            Certifications: "Certifications", Languages: "Languages", Present: "Present", Project: "Project", Technologies: "Technologies"),
        [CvLanguage.De] = new(
            Summary: "Zusammenfassung", Skills: "Kenntnisse", Experience: "Berufserfahrung", Education: "Ausbildung",
            Certifications: "Zertifizierungen", Languages: "Sprachen", Present: "Aktuell", Project: "Projekt", Technologies: "Technologien"),
        [CvLanguage.Ua] = new(
            Summary: "Резюме", Skills: "Навички", Experience: "Досвід роботи", Education: "Освіта",
            Certifications: "Сертифікати", Languages: "Мови", Present: "Дотепер", Project: "Проєкт", Technologies: "Технології"),
        [CvLanguage.Ru] = new(
            Summary: "Резюме", Skills: "Навыки", Experience: "Опыт работы", Education: "Образование",
            Certifications: "Сертификаты", Languages: "Языки", Present: "По настоящее время", Project: "Проект", Technologies: "Технологии"),
        [CvLanguage.Es] = new(
            Summary: "Resumen", Skills: "Habilidades", Experience: "Experiencia", Education: "Educación",
            Certifications: "Certificaciones", Languages: "Idiomas", Present: "Presente", Project: "Proyecto", Technologies: "Tecnologías"),
    };

    private static readonly Dictionary<CvLanguage, CultureInfo> CultureByLanguage = new()
    {
        [CvLanguage.En] = CultureInfo.GetCultureInfo("en-US"),
        [CvLanguage.De] = CultureInfo.GetCultureInfo("de-DE"),
        [CvLanguage.Ua] = CultureInfo.GetCultureInfo("uk-UA"),
        [CvLanguage.Ru] = CultureInfo.GetCultureInfo("ru-RU"),
        [CvLanguage.Es] = CultureInfo.GetCultureInfo("es-ES"),
    };
}
