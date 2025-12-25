(function () {
  function onViewShow(e) {
    if (!e.target || !e.target.classList || !e.target.classList.contains('homePage')) return;
    renderResumeRow();
  }

  document.addEventListener('viewshow', onViewShow);
  ensureStyles();

  async function renderResumeRow() {
    var apiClient = window.ApiClient;
    if (!apiClient) return;

    var container = document.querySelector('.homeSectionsContainer');
    if (!container) return;

    var existing = container.querySelector('.pnu-resume-row');
    if (existing) existing.remove();

    var candidates;
    try {
      candidates = await apiClient.getJSON(apiClient.getUrl('PlaylistNextUp/Resume'));
    } catch (err) {
      console.debug('[PlaylistNextUp] Resume fetch failed', err);
      return;
    }

    if (!candidates || !candidates.length) return;

    var userId = apiClient.getCurrentUserId();
    var ids = candidates.map(function (c) { return c.nextItemId; }).join(',');
    var itemsResp;
    try {
      itemsResp = await apiClient.getJSON(apiClient.getUrl('Users/' + userId + '/Items', {
        Ids: ids,
        Fields: 'PrimaryImageAspectRatio'
      }));
    } catch (err) {
      console.debug('[PlaylistNextUp] Items fetch failed', err);
      return;
    }

    var items = (itemsResp && itemsResp.Items) ? itemsResp.Items : [];
    var byId = {};
    items.forEach(function (i) { byId[i.Id] = i; });

    var section = document.createElement('section');
    section.className = 'homeSection pnu-resume-row';
    section.innerHTML = '' +
      '<div class="sectionTitleContainer">' +
      '  <h2 class="sectionTitle">Continue playlists/collections</h2>' +
      '</div>' +
      '<div class="pnu-row"></div>';

    var row = section.querySelector('.pnu-row');

    candidates.forEach(function (c) {
      var item = byId[c.nextItemId];
      if (!item) return;

      var typeLabel = c.containerType === 1 ? 'Collection' : 'Playlist';
      var card = document.createElement('a');
      card.className = 'pnu-card';
      card.href = '#!/details?id=' + item.Id;

      var imgUrl = apiClient.getImageUrl(item.Id, {
        type: 'Primary',
        maxWidth: 320,
        quality: 90
      });

      var title = item.Name || 'Item';
      card.innerHTML = '' +
        '<div class="pnu-thumb" style="background-image:url(\\'' + imgUrl + '\\')"></div>' +
        '<div class="pnu-title">' + title + '</div>' +
        '<div class="pnu-meta">' + typeLabel + '</div>';

      row.appendChild(card);
    });

    if (!row.children.length) return;

    container.prepend(section);
  }

  function ensureStyles() {
    if (document.getElementById('pnu-styles')) return;
    var style = document.createElement('style');
    style.id = 'pnu-styles';
    style.textContent = '' +
      '.pnu-row{display:flex;gap:16px;overflow-x:auto;padding:8px 0 16px;}' +
      '.pnu-card{display:block;min-width:160px;max-width:200px;color:inherit;text-decoration:none;}' +
      '.pnu-thumb{width:100%;aspect-ratio:2/3;background:#222 center/cover;border-radius:8px;}' +
      '.pnu-title{margin-top:8px;font-size:0.9em;line-height:1.2;}' +
      '.pnu-meta{margin-top:4px;font-size:0.75em;opacity:0.7;}';
    document.head.appendChild(style);
  }
})();
