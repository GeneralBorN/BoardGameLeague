// League Assistant chat widget: talks to /api/chat, which runs a Gemini
// function-calling loop server-side. What the assistant is allowed to do
// (search only, vs. search + create) is decided entirely server-side from
// the signed-in user's role - this file just renders whatever comes back.
//
// The conversation is persisted to localStorage (keyed per signed-in user, so
// a shared computer doesn't leak one account's chat into another's) so it
// survives normal full-page navigation across this MVC app, not just
// client-side routing. It's still just a browser-local convenience, not
// synced anywhere server-side.
(function () {
    const widget = document.getElementById("chat-widget");
    if (!widget) return;

    const toggleBtn = document.getElementById("chat-toggle");
    const closeBtn = document.getElementById("chat-close");
    const clearBtn = document.getElementById("chat-clear");
    const panel = document.getElementById("chat-panel");
    const messages = document.getElementById("chat-messages");
    const form = document.getElementById("chat-form");
    const input = document.getElementById("chat-input");
    const csrfToken = widget.dataset.csrfToken;

    const storageKey = "boardgameleague.chat." + (widget.dataset.userId || "anonymous");
    const maxStoredTurns = 40;

    let turns = loadTurns();
    let busy = false;

    turns.forEach(function (turn) {
        renderMessage(turn.role, turn.text, turn.links);
    });

    toggleBtn.addEventListener("click", function () {
        panel.classList.toggle("d-none");
        if (!panel.classList.contains("d-none")) {
            input.focus();
        }
    });

    closeBtn.addEventListener("click", function () {
        panel.classList.add("d-none");
    });

    clearBtn.addEventListener("click", function () {
        if (!confirm("Clear this conversation? This can't be undone.")) {
            return;
        }
        turns = [];
        saveTurns();
        messages.innerHTML = "";
    });

    function loadTurns() {
        try {
            const raw = localStorage.getItem(storageKey);
            return raw ? JSON.parse(raw) : [];
        } catch (e) {
            return [];
        }
    }

    function saveTurns() {
        try {
            const trimmed = turns.slice(-maxStoredTurns);
            localStorage.setItem(storageKey, JSON.stringify(trimmed));
        } catch (e) {
            // Storage full or unavailable (e.g. private browsing) - conversation
            // still works for this page view, it just won't persist across nav.
        }
    }

    function renderMessage(role, text, links) {
        const row = document.createElement("div");
        row.className = "chat-message chat-message-" + role;

        const bubble = document.createElement("div");
        bubble.className = "chat-bubble";
        bubble.textContent = text;
        row.appendChild(bubble);

        if (links && links.length > 0) {
            const linkWrap = document.createElement("div");
            linkWrap.className = "chat-links";
            links.forEach(function (link) {
                const a = document.createElement("a");
                a.href = link.url;
                a.textContent = link.label;
                a.className = "chat-link-chip";
                linkWrap.appendChild(a);
            });
            row.appendChild(linkWrap);
        }

        messages.appendChild(row);
        messages.scrollTop = messages.scrollHeight;
    }

    function addTurn(role, text, links) {
        turns.push({ role: role, text: text, links: links || [] });
        saveTurns();
        renderMessage(role, text, links);
    }

    function setBusy(value) {
        busy = value;
        input.disabled = value;
        form.querySelector("button[type=submit]").disabled = value;
    }

    form.addEventListener("submit", function (e) {
        e.preventDefault();
        if (busy) return;

        const text = input.value.trim();
        if (!text) return;

        // Snapshot history BEFORE adding this new message: the server appends
        // `message` as the final turn itself, so including it here too would
        // duplicate it in what gets sent to Gemini.
        const history = turns.map(function (t) {
            return { role: t.role, text: t.text };
        });

        addTurn("user", text);
        input.value = "";
        setBusy(true);

        fetch("/api/chat", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "X-CSRF-TOKEN": csrfToken
            },
            body: JSON.stringify({ message: text, history: history })
        })
            .then(function (response) {
                if (!response.ok) {
                    throw new Error("Request failed with status " + response.status);
                }
                return response.json();
            })
            .then(function (data) {
                addTurn("model", data.reply, data.links);
            })
            .catch(function () {
                addTurn("model", "Sorry, something went wrong reaching the assistant. Please try again.");
            })
            .finally(function () {
                setBusy(false);
                input.focus();
            });
    });
})();
