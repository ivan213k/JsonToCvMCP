using System.Globalization;
using JsonToCvApi.Models;

namespace JsonToCvApi.Localization;

public interface ICvLocalizationProvider
{
    CvLabels GetLabels(CvLanguage language);

    CultureInfo GetCulture(CvLanguage language);
}
