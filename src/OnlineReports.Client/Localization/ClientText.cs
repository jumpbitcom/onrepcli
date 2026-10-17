// Centralizes the OnlineReports client interface text in its three supported languages.
namespace OnlineReports.Client.Localization;

public sealed record ClientText(
    string Tagline,
    string Refresh,
    string SyncInformation,
    string LastUpdated,
    string NeverUpdated,
    string ArticlesLoaded,
    string NewStory,
    string Updated,
    string SomeArticlesFailed,
    string Updating,
    string UpdateFailed,
    string Stories,
    string NoStories,
    string SelectStory,
    string BackToStories,
    string Published,
    string By,
    string Sections,
    string LoadImage,
    string OpenInBrowser,
    string MediaLinks,
    string FullTextUnavailable,
    string AllStories,
    string News,
    string Politics,
    string Economy,
    string Culture,
    string Columns)
{
    public static ClientText For(string language) =>
        language switch
        {
            "Français" => new(
                "Macht bekannt.", "Actualiser", "Informations de synchronisation", "Dernière mise à jour",
                "Jamais mis à jour", "{0} article(s) chargés.", "Nouvel article",
                "Actualisé : {0} article(s).",
                "{0} article(s) n’ont pas pu être téléchargé(s) : {1}", "Mise à jour en cours…",
                "Échec de la mise à jour : {0}", "Articles", "Aucun article dans cette rubrique.",
                "Sélectionnez un article pour le lire.", "Retour aux articles", "Publié le", "Par",
                "Rubriques", "Charger l’image", "Lire l’article sur le site", "Liens médias",
                "Le texte intégral n’est pas disponible hors ligne.", "Tous les articles", "Actualités",
                "Politique", "Économie", "Culture et société", "Chroniques"),
            "English" => new(
                "Macht bekannt.", "Update", "Sync information", "Last updated", "Never updated",
                "{0} articles loaded.", "New story", "Updated: {0} article(s).",
                "{0} article(s) could not be downloaded: {1}",
                "Updating…", "Update failed: {0}", "Stories",
                "No stories in this section.", "Select a story to read it.", "Back to stories",
                "Published", "By", "Sections", "Load image", "Read the article on the website", "Media links",
                "Full text is not available offline.", "All stories", "News", "Politics", "Economy",
                "Culture & Society", "Columns"),
            _ => new(
                "Macht bekannt.", "Aktualisieren", "Synchronisationsinfo", "Letzte Aktualisierung",
                "Noch nie aktualisiert", "{0} Artikel geladen.", "Neuer Artikel",
                "Aktualisiert: {0} Artikel.",
                "{0} Artikel konnten nicht geladen werden: {1}", "Wird aktualisiert…",
                "Aktualisierung fehlgeschlagen: {0}", "Artikel", "Keine Artikel in diesem Ressort.",
                "Wähle einen Artikel zum Lesen.", "Zurück zur Übersicht", "Publiziert", "Von",
                "Ressorts", "Bild laden", "Artikel auf der Website lesen", "Medienlinks",
                "Der vollständige Text ist offline nicht verfügbar.", "Alle Artikel", "News",
                "Politik", "Wirtschaft", "Kultur & Gesellschaft", "Kolumnen")
        };
}

public sealed record SectionOption(string Key, string Title);
