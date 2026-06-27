document.addEventListener('DOMContentLoaded', function () {
    const searchInput = document.getElementById('global-search-input');
    const searchResults = document.getElementById('global-search-results');
    let debounceTimer;

    searchInput.addEventListener('input', function () {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            const query = this.value;
            if (query.length < 3) {
                searchResults.classList.remove('show');
                return;
            }

            fetch(`/api/search?q=${encodeURIComponent(query)}`)
                .then(response => response.json())
                .then(data => {
                    renderSearchResults(data);
                });
        }, 300);
    });

    function renderSearchResults(data) {
        searchResults.innerHTML = '';
        let hasResults = false;

        const categories = {
            "Tournaments": data.tournaments,
            "Players": data.players,
            "Teams": data.teams,
            "Board Games": data.boardGames,
            "Venues": data.venues
        };

        for (const category in categories) {
            const items = categories[category];
            if (items && items.length > 0) {
                hasResults = true;
                const header = document.createElement('h6');
                header.className = 'dropdown-header';
                header.textContent = category;
                searchResults.appendChild(header);

                items.forEach(item => {
                    const link = document.createElement('a');
                    link.className = 'dropdown-item';
                    link.href = `/${item.type}s/Details/${item.id}`;
                    link.textContent = item.name;
                    searchResults.appendChild(link);
                });
            }
        }

        if (hasResults) {
            searchResults.classList.add('show');
        } else {
            const noResults = document.createElement('span');
            noResults.className = 'dropdown-item-text text-muted';
            noResults.textContent = 'No results found';
            searchResults.appendChild(noResults);
            searchResults.classList.add('show');
        }
    }

    document.addEventListener('click', function (e) {
        if (!searchInput.contains(e.target)) {
            searchResults.classList.remove('show');
        }
    });
});
