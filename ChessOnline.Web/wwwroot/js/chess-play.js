(function () {
    const lobbyId = window.chessModel?.LobbyId;
    const currentUserId = window.chessModel?.CurrentUserId;

    const game = new Chess();
    let board;

    // clocks (seconds)
    let whiteRemaining = 0;
    let blackRemaining = 0;
    let whitePlayerId = null;
    let blackPlayerId = null;

    let myColor = null; // 'w' or 'b'
    let tickingInterval = null;
    let isMyTurn = false;

    function secToText(s) {
        const m = Math.floor(s / 60).toString().padStart(2, '0');
        const sec = (s % 60).toString().padStart(2, '0');
        return `${m}:${sec}`;
    }

    function updateClocksUI() {
        document.getElementById('whiteClock').innerText = secToText(Math.max(0, whiteRemaining));
        document.getElementById('blackClock').innerText = secToText(Math.max(0, blackRemaining));
    }

    function startTicking() {
        stopTicking();
        tickingInterval = setInterval(() => {
            if (game.turn() === 'w') {
                whiteRemaining = Math.max(0, whiteRemaining - 1);
            } else {
                blackRemaining = Math.max(0, blackRemaining - 1);
            }
            updateClocksUI();
        }, 1000);
    }

    function stopTicking() {
        if (tickingInterval) {
            clearInterval(tickingInterval);
            tickingInterval = null;
        }
    }

    // fetch initial state (fen + clocks)
    fetch(`/api/game/state/${lobbyId}`)
        .then(r => r.json())
        .then(s => {
            if (s && s.fen) {
                try { game.load(s.fen); } catch (e) { console.error(e); }
            }
            whitePlayerId = s.whitePlayerId;
            blackPlayerId = s.blackPlayerId;
            whiteRemaining = s.whiteRemainingSeconds ?? 300;
            blackRemaining = s.blackRemainingSeconds ?? 300;

            // determine client color
            if (currentUserId === whitePlayerId) myColor = 'w';
            else if (currentUserId === blackPlayerId) myColor = 'b';

            document.getElementById('whiteName').innerText = whitePlayerId ?? 'White';
            document.getElementById('blackName').innerText = blackPlayerId ?? 'Black';

            updateClocksUI();
            initBoard();
            startTicking();
            updateStatus();
        })
        .catch(err => {
            console.error(err);
            initBoard();
        });

    function initBoard() {
        const config = {
            draggable: true,
            position: game.fen(),
            onDragStart: function (source, piece, position, orientation) {
                if (game.game_over()) return false;
                if (myColor === null) return false;
                if ((myColor === 'w' && piece.startsWith('b')) || (myColor === 'b' && piece.startsWith('w'))) return false;
                if ((myColor === 'w' && game.turn() !== 'w') || (myColor === 'b' && game.turn() !== 'b'))
                    return false;
                return true;
            },
            onDrop: function (source, target) {
                const move = game.move({ from: source, to: target, promotion: 'q' });
                if (move === null) return 'snapback';

                fetch('/api/game/move', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    credentials: 'same-origin',
                    body: JSON.stringify({
                        lobbyId: lobbyId,
                        from: source,
                        to: target,
                        whiteRemainingSeconds: whiteRemaining,
                        blackRemainingSeconds: blackRemaining
                    })
                })
                    .then(res => res.json())
                    .then(data => {
                        if (data && data.success) {
                            if (data.fen) {
                                game.load(data.fen);
                                board.position(data.fen);
                            } else {
                                board.position(game.fen());
                            }
                            updateStatus();
                        } else {
                            game.undo();
                            board.position(game.fen());
                            alert(data?.message || 'Move rejected by server');
                        }
                    })
                    .catch(err => {
                        console.error(err);
                        game.undo();
                        board.position(game.fen());
                        alert('Network error');
                    });

                return;
            },
            onSnapEnd: function () {
                board.position(game.fen());
            }
        };

        board = Chessboard('board', config);
    }

    function updateStatus() {
        let status = '';
        const turn = game.turn() === 'w' ? 'White' : 'Black';
        status = 'Turn: ' + turn;
        if (game.in_checkmate()) {
            status = 'Game over, checkmate.';
            stopTicking();
        } else if (game.in_draw()) {
            status = 'Game over, draw.';
            stopTicking();
        } else if (game.in_check()) {
            status += ' — Check!';
        }
        document.getElementById('status').innerText = status;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/chessHub')
        .withAutomaticReconnect()
        .build();

    connection.on('ReceiveMove', function (incomingLobbyId, fen, move, isGameOver, wRemain, bRemain) {
        if (incomingLobbyId !== lobbyId) return;
        try {
            if (fen) {
                game.load(fen);
                board.position(fen);
            }
            if (typeof wRemain === 'number') whiteRemaining = wRemain;
            if (typeof bRemain === 'number') blackRemaining = bRemain;
            updateClocksUI();
            updateStatus();
        } catch (e) { console.error(e); }
    });

    connection.start()
        .then(() => connection.invoke('JoinLobby', lobbyId).catch(err => console.error(err.toString())))
        .catch(err => console.error(err.toString()));

    window.addEventListener('beforeunload', function () {
        try { connection.invoke('LeaveLobby', lobbyId); } catch { }
    });

    setInterval(() => {
        fetch('/api/game/sync-clock', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'same-origin',
            body: JSON.stringify({ lobbyId: lobbyId, whiteRemainingSeconds: whiteRemaining, blackRemainingSeconds: blackRemaining })
        }).catch(() => { });
    }, 5000);

})();
