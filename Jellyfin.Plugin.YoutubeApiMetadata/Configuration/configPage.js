const YoutubeApiMetadataConfig = {
    pluginUniqueId: '338cccea-4c27-474e-8934-4c7c3737d034'
};

export default function (view) {
    view.addEventListener('viewshow', function () {
        Dashboard.showLoadingMsg();
        const page = this;

        ApiClient.getPluginConfiguration(YoutubeApiMetadataConfig.pluginUniqueId).then(function (config) {
            page.querySelector('#apiKey').value = config.ApiKey || '';
            page.querySelector('#cacheExpirationDays').value = config.CacheExpirationDays;
            Dashboard.hideLoadingMsg();
        }).catch(function () {
            Dashboard.hideLoadingMsg();
            Dashboard.processErrorResponse({ statusText: 'Failed to load plugin configuration' });
        });
    });

    view.querySelector('#YoutubeApiMetadataConfigForm').addEventListener('submit', function (e) {
        e.preventDefault();
        Dashboard.showLoadingMsg();
        const form = this;

        ApiClient.getPluginConfiguration(YoutubeApiMetadataConfig.pluginUniqueId).then(function (config) {
            config.ApiKey = form.querySelector('#apiKey').value.trim();
            config.CacheExpirationDays = parseInt(form.querySelector('#cacheExpirationDays').value, 10);

            ApiClient.updatePluginConfiguration(YoutubeApiMetadataConfig.pluginUniqueId, config).then(function (result) {
                Dashboard.processPluginConfigurationUpdateResult(result);
            }).catch(function () {
                Dashboard.hideLoadingMsg();
                Dashboard.processErrorResponse({ statusText: 'Failed to update plugin configuration' });
            });
        }).catch(function () {
            Dashboard.hideLoadingMsg();
            Dashboard.processErrorResponse({ statusText: 'Failed to load plugin configuration' });
        });

        return false;
    });
}
