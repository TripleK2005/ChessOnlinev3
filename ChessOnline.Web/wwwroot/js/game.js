// Game Logic
const lobbyId = document.getElementById('lobbyId').value;
const currentUserId = document.getElementById('currentUserId').value;

let board = null;
let game = new Chess();
let connection = null;
let playerColor = 'white'; // default, will be set from state
let isSpectator = false;
let whitePlayerId = null;
let blackPlayerId = null;

// Clock
let whiteTime = 300;
let blackTime = 300;
let timerInterval = null;
let isGameActive = false;

// UI Elements
const statusEl = document.getElementById('gameStatus');
const topClock = document.getElementById('topClock');
const bottomClock = document.getElementById('bottomClock');
const topName = document.getElementById('topPlayerName');
const bottomName = document.getElementById('bottomPlayerName');
const moveListBody = document.getElementById('moveListBody');
const chatMessages = document.getElementById('chatMessages');
const chatForm = document.getElementById('chatForm');
const chatInput = document.getElementById('chatInput');

// Initialize SignalR
connection = new signalR.HubConnectionBuilder()
    .withUrl("/chessHub")
    .build();

connection.on("ReceiveMove", (fen) => {
    game.load(fen);
    board.position(fen);
    updateStatus();
    updateMoveHistory();
    // Clock sync will happen via separate calls or implied
});

connection.on("ReceiveMessage", (user, message) => {
    const div = document.createElement('div');
    div.className = 'chat-message';
    div.innerHTML = `<span class="chat-user">${user}:</span> ${escapeHtml(message)}`;
    chatMessages.appendChild(div);
    chatMessages.scrollTop = chatMessages.scrollHeight;
});

connection.on("UpdateClock", (w, b) => {
    whiteTime = w;
    blackTime = b;
    updateClocksUI();
});

connection.start().then(async () => {
    console.log("SignalR Connected");
    await connection.invoke("JoinLobby", lobbyId);
    await loadGameState();
}).catch(err => console.error(err));

// Board Config
const config = {
    draggable: true,
    position: 'start',
    pieceTheme: 'https://cdnjs.cloudflare.com/ajax/libs/chessboard-js/1.0.0/img/chesspieces/wikipedia/{piece}.png',
    onDragStart: onDragStart,
    onDrop: onDrop,
    onSnapEnd: onSnapEnd
};

try {
    if (typeof Chessboard === 'undefined') {
        throw new Error("Chessboard library not loaded!");
    }
    board = Chessboard('myBoard', config);
} catch (e) {
    console.error("Chessboard Init Failed:", e);
    alert("Failed to initialize Chessboard: " + e.message);
}

// Game Functions

async function loadGameState() {
    try {
        const res = await fetch(`/state/${lobbyId}`);
        const state = await res.json();

        game.load(state.fen);
        board.position(state.fen);

        whitePlayerId = state.whitePlayerId;
        blackPlayerId = state.blackPlayerId;

        // Determine orientation
        if (currentUserId === whitePlayerId) {
            playerColor = 'white';
            board.orientation('white');
            bottomName.textContent = `You (${state.whitePlayerName})`;
            topName.textContent = `${state.blackPlayerName} (Black)`;
        } else if (currentUserId === blackPlayerId) {
            playerColor = 'black';
            board.orientation('black');
            bottomName.textContent = `You (${state.blackPlayerName})`;
            topName.textContent = `${state.whitePlayerName} (White)`;
        } else {
            isSpectator = true;
            playerColor = 'spectator';
            bottomName.textContent = `${state.whitePlayerName} (White)`;
            topName.textContent = `${state.blackPlayerName} (Black)`;
        }

        if (state.whiteRemainingSeconds !== null) whiteTime = state.whiteRemainingSeconds;
        if (state.blackRemainingSeconds !== null) blackTime = state.blackRemainingSeconds;

        isGameActive = !state.isGameOver;
        updateStatus();
        updateMoveHistory();
        updateClocksUI();

        if (isGameActive) startTimer();

    } catch (err) {
        console.error("Failed to load state", err);
    }
}

function onDragStart(source, piece, position, orientation) {
    if (game.game_over()) return false;
    if (isSpectator) return false;

    // only pick up pieces for the side to move
    if ((game.turn() === 'w' && piece.search(/^b/) !== -1) ||
        (game.turn() === 'b' && piece.search(/^w/) !== -1)) {
        return false;
    }

    // only pick up own pieces
    if ((game.turn() === 'w' && playerColor !== 'white') ||
        (game.turn() === 'b' && playerColor !== 'black')) {
        return false;
    }
}

async function onDrop(source, target) {
    // see if the move is legal
    const move = game.move({
        from: source,
        to: target,
        promotion: 'q' // NOTE: always promote to a queen for example simplicity
    });

    // illegal move
    if (move === null) return 'snapback';

    updateStatus();
    updateMoveHistory();

    // Send move to server
    try {
        const payload = {
            lobbyId: lobbyId,
            from: source,
            to: target,
            whiteRemainingSeconds: whiteTime,
            blackRemainingSeconds: blackTime
        };

        const res = await fetch('/move', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        const json = await res.json();
        if (!json.success) {
            console.error("Move failed", json.message);
            game.undo(); // undo local move
            board.position(game.fen());
            return 'snapback';
        }
    } catch (err) {
        console.error(err);
        game.undo();
        board.position(game.fen());
        return 'snapback';
    }
}

function onSnapEnd() {
    board.position(game.fen());
}

function updateStatus() {
    let status = '';

    let moveColor = 'White';
    if (game.turn() === 'b') {
        moveColor = 'Black';
    }

    if (game.in_checkmate()) {
        status = 'Game over, ' + moveColor + ' is in checkmate.';
        isGameActive = false;
        showGameOver(moveColor === 'White' ? 'Black' : 'White', 'Checkmate');
    } else if (game.in_draw()) {
        status = 'Game over, drawn position';
        isGameActive = false;
        showGameOver('Draw', 'Draw');
    } else {
        status = moveColor + ' to move';
        if (game.in_check()) {
            status += ', ' + moveColor + ' is in check';
        }
    }

    statusEl.textContent = status;
}

function updateMoveHistory() {
    const history = game.history({ verbose: true });
    moveListBody.innerHTML = '';

    for (let i = 0; i < history.length; i += 2) {
        const moveWhite = history[i];
        const moveBlack = history[i + 1];

        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${Math.floor(i / 2) + 1}</td>
            <td>${moveWhite.san}</td>
            <td>${moveBlack ? moveBlack.san : ''}</td>
        `;
        moveListBody.appendChild(row);
    }

    // Scroll to bottom
    const container = document.getElementById('moveHistory');
    container.scrollTop = container.scrollHeight;
}

// Clock Logic
function startTimer() {
    if (timerInterval) clearInterval(timerInterval);

    timerInterval = setInterval(() => {
        if (!isGameActive) return;

        if (game.turn() === 'w') {
            whiteTime--;
        } else {
            blackTime--;
        }

        if (whiteTime <= 0 || blackTime <= 0) {
            isGameActive = false;
            clearInterval(timerInterval);
            // Server handles timeout logic usually, but we can show UI
        }

        updateClocksUI();
    }, 1000);
}

function updateClocksUI() {
    const format = (t) => {
        const m = Math.floor(t / 60);
        const s = t % 60;
        return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
    };

    const wStr = format(Math.max(0, whiteTime));
    const bStr = format(Math.max(0, blackTime));

    if (playerColor === 'white') {
        bottomClock.textContent = wStr;
        topClock.textContent = bStr;
        bottomClock.classList.toggle('active', game.turn() === 'w');
        topClock.classList.toggle('active', game.turn() === 'b');
    } else {
        bottomClock.textContent = bStr;
        topClock.textContent = wStr;
        bottomClock.classList.toggle('active', game.turn() === 'b');
        topClock.classList.toggle('active', game.turn() === 'w');
    }
}

function showGameOver(winner, reason) {
    document.getElementById('winnerText').textContent = winner === 'Draw' ? 'Game Drawn' : `${winner} Wins!`;
    document.getElementById('reasonText').textContent = reason;
    new bootstrap.Modal(document.getElementById('gameOverModal')).show();
}

// Chat
chatForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    const msg = chatInput.value.trim();
    if (!msg) return;

    try {
        await connection.invoke("SendMessage", lobbyId, "Me", msg); // "Me" should be replaced by username if possible
        chatInput.value = '';
    } catch (err) {
        console.error(err);
    }
});

// Flip Board
document.getElementById('flipBtn').addEventListener('click', () => {
    board.flip();
    // Swap clocks visually if needed, but usually clocks stay with player names
});

function escapeHtml(text) {
    if (!text) return text;
    return text
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}
