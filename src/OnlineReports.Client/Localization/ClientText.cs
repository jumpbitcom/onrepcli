// Centralizes the OnlineReports client interface text in its three supported languages.
namespace OnlineReports.Client.Localization;

public sealed record ClientText(
    string Tagline,
    string Refresh,
    string SyncInfoButton,
    string LastUpdated,
    string NeverUpdated,
    string ArticlesLoaded,
    string NewStory,
    string UpdateWarning,
    string Updating,
    string UpdateFailed,
    string Stories,
    string NoStories,
    string BackToStories,
    string Published,
    string By,
    string Sections,
    string OpenInBrowser,
    string MediaLinks,
    string FullTextUnavailable,
    string TopNews,
    string News,
    string Politics,
    string Economy,
    string Culture,
    string Columns,
    string AllArticles)
{
    public static ClientText For(string language) =>
        language switch
        {
            "Français" => new(
                "Macht bekannt.", "Actualiser", "Informations de synchronisation",
                "Dernière mise à jour", "Jamais mis à jour", "{0} article(s) chargés.", "Nouvel article",
                "{0} article(s) n’ont pas pu être téléchargé(s) : {1}",
                "Mise à jour en cours…",
                "Erreur lors de la mise à jour : {0}", "Articles", "Aucun article dans cette rubrique.",
                "Retour aux articles", "Publié le", "Par",
                "Rubriques", "Lire l’article sur le site", "Liens médias",
                "Le texte intégral n’est pas disponible hors ligne.", "À la une", "Actualités",
                "Politique", "Économie", "Culture et société", "Chroniques", "Tous les articles"),
            "English" => new(
                "Macht bekannt.", "Update", "Sync information",
                "Last updated", "Never updated", "{0} articles loaded.", "New story",
                "Warning: {0} article(s) could not be downloaded: {1}",
                "Updating…", "Error updating: {0}", "Stories",
                "No stories in this section.", "Back to stories",
                "Published", "By", "Sections", "Read the article on the website", "Media links",
                "Full text is not available offline.", "Top News", "News", "Politics", "Economy",
                "Culture & Society", "Columns", "All articles"),
            _ => new(
                "Macht bekannt.", "Aktualisieren", "Synchronisationsinfo",
                "Letzte Aktualisierung", "Noch nie aktualisiert", "{0} Artikel geladen.", "Neuer Artikel",
                "Warnung: {0} Artikel konnten nicht geladen werden: {1}",
                "Wird aktualisiert…",
                "Fehler bei der Aktualisierung: {0}", "Artikel", "Keine Artikel in diesem Ressort.",
                "Zurück zur Übersicht", "Publiziert", "Von",
                "Ressorts", "Artikel auf der Website lesen", "Medienlinks",
                "Der vollständige Text ist offline nicht verfügbar.", "Top News", "News",
                "Politik", "Wirtschaft", "Kultur & Gesellschaft", "Kolumnen", "Alle Artikel")
        };
}

public sealed record SectionOption(string Key, string Title);
