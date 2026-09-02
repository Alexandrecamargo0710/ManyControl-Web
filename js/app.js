let tokenClient = null;
let googleAccessToken = localStorage.getItem('manycontrol_google_token') || null;

window.manyControlJs = {
    downloadFile: function (filename, content, mimeType) {
        const blob = new Blob([content], { type: mimeType || 'application/json;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    },

    triggerFileInput: function (elementId) {
        const elem = document.getElementById(elementId);
        if (elem) {
            elem.click();
        }
    },

    isIos: function () {
        const userAgent = window.navigator.userAgent.toLowerCase();
        return /iphone|ipad|ipod/.test(userAgent);
    },

    isStandalone: function () {
        return ('standalone' in window.navigator && window.navigator.standalone) ||
               window.matchMedia('(display-mode: standalone)').matches;
    },

    vibrate: function (duration) {
        if ('vibrate' in navigator) {
            navigator.vibrate(duration || 40);
        }
    },

    forceUpdateAndReload: async function () {
        try {
            if ('caches' in window) {
                const keys = await caches.keys();
                for (const key of keys) {
                    await caches.delete(key);
                }
            }
            if ('serviceWorker' in navigator) {
                const registrations = await navigator.serviceWorker.getRegistrations();
                for (const reg of registrations) {
                    await reg.unregister();
                }
            }
        } catch (e) {
            console.error('Erro ao limpar cache:', e);
        }
        window.location.reload(true);
    },

    setTheme: function (theme) {
        const t = theme === 'light' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-theme', t);
        localStorage.setItem('manycontrol_theme', t);
        
        // Mantém a cor de destaque personalizada se houver, ou remove o override
        const customAccent = localStorage.getItem('manycontrol_accent_color');
        if (customAccent) {
            document.documentElement.style.setProperty('--accent-color', customAccent);
            document.documentElement.style.setProperty('--bottom-nav-active', customAccent);
            document.documentElement.style.setProperty('--color-blue-btn', customAccent);
        } else {
            document.documentElement.style.removeProperty('--accent-color');
            document.documentElement.style.removeProperty('--bottom-nav-active');
            document.documentElement.style.removeProperty('--color-blue-btn');
        }

        // Atualiza a meta tag theme-color no header
        const metaThemeColor = document.querySelector('meta[name="theme-color"]');
        if (metaThemeColor) {
            metaThemeColor.setAttribute('content', t === 'light' ? '#f1f5f9' : '#0b0f19');
        }
    },

    getTheme: function () {
        return localStorage.getItem('manycontrol_theme') || 'dark';
    },

    setAccentColor: function (color) {
        if (color && color.trim() !== '') {
            document.documentElement.style.setProperty('--accent-color', color.trim());
            document.documentElement.style.setProperty('--bottom-nav-active', color.trim());
            document.documentElement.style.setProperty('--color-blue-btn', color.trim());
            localStorage.setItem('manycontrol_accent_color', color.trim());
        } else {
            document.documentElement.style.removeProperty('--accent-color');
            document.documentElement.style.removeProperty('--bottom-nav-active');
            document.documentElement.style.removeProperty('--color-blue-btn');
            localStorage.removeItem('manycontrol_accent_color');
        }
    },

    getAccentColor: function () {
        return localStorage.getItem('manycontrol_accent_color') || '';
    }
};

window.manyControlGoogleDrive = {
    init: function(clientId) {
        if (!window.google || !window.google.accounts || !window.google.accounts.oauth2) return;
        tokenClient = window.google.accounts.oauth2.initTokenClient({
            client_id: clientId,
            scope: 'https://www.googleapis.com/auth/drive.file https://www.googleapis.com/auth/userinfo.email',
            callback: (tokenResponse) => {
                if (tokenResponse && tokenResponse.access_token) {
                    googleAccessToken = tokenResponse.access_token;
                    localStorage.setItem('manycontrol_google_token', googleAccessToken);
                    if (window._googleDotNetRef) {
                        window._googleDotNetRef.invokeMethodAsync('OnGoogleAuthSuccess', googleAccessToken);
                    }
                }
            }
        });
    },

    requestToken: function(dotNetRef, clientId) {
        window._googleDotNetRef = dotNetRef;
        if (!tokenClient && clientId) {
            this.init(clientId);
        }
        if (tokenClient) {
            tokenClient.requestAccessToken({ prompt: 'consent' });
        } else if (window.google && window.google.accounts && window.google.accounts.oauth2) {
            this.init(clientId);
            if (tokenClient) {
                tokenClient.requestAccessToken({ prompt: 'consent' });
            }
        } else {
            alert('Aguarde o carregamento do serviço do Google ou verifique se bloqueadores de pop-up estão ativos.');
        }
    },

    disconnect: function() {
        if (googleAccessToken && window.google && window.google.accounts && window.google.accounts.oauth2) {
            window.google.accounts.oauth2.revoke(googleAccessToken, () => {});
        }
        googleAccessToken = null;
        localStorage.removeItem('manycontrol_google_token');
        localStorage.removeItem('manycontrol_google_email');
    },

    getUserEmail: async function(token) {
        const t = token || googleAccessToken;
        if (!t) return null;
        try {
            const res = await fetch('https://www.googleapis.com/oauth2/v2/userinfo', {
                headers: { Authorization: `Bearer ${t}` }
            });
            if (res.ok) {
                const data = await res.json();
                localStorage.setItem('manycontrol_google_email', data.email);
                return data.email;
            }
        } catch (e) {
            console.error('Erro ao buscar email do Google:', e);
        }
        return null;
    },

    syncDrive: async function(jsonContent) {
        const t = googleAccessToken || localStorage.getItem('manycontrol_google_token');
        if (!t) throw new Error('Não autenticado com o Google Drive.');
        
        // 1. Procurar ou criar pasta "ManyControl"
        const folderQuery = encodeURIComponent("name = 'ManyControl' and mimeType = 'application/vnd.google-apps.folder' and trashed = false");
        let folderRes = await fetch(`https://www.googleapis.com/drive/v3/files?q=${folderQuery}&fields=files(id, name)`, {
            headers: { Authorization: `Bearer ${t}` }
        });
        
        if (folderRes.status === 401) {
            localStorage.removeItem('manycontrol_google_token');
            throw new Error('Sessão expirada. Por favor, conecte-se novamente com o Google.');
        }

        let folderData = await folderRes.json();
        let folderId = null;

        if (folderData.files && folderData.files.length > 0) {
            folderId = folderData.files[0].id;
        } else {
            const createFolderRes = await fetch('https://www.googleapis.com/drive/v3/files', {
                method: 'POST',
                headers: {
                    Authorization: `Bearer ${t}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    name: 'ManyControl',
                    mimeType: 'application/vnd.google-apps.folder'
                })
            });
            const newFolder = await createFolderRes.json();
            folderId = newFolder.id;
        }

        // 2. Procurar arquivo "manycontrol-sync.json" na pasta
        const fileQuery = encodeURIComponent(`name = 'manycontrol-sync.json' and '${folderId}' in parents and trashed = false`);
        const fileRes = await fetch(`https://www.googleapis.com/drive/v3/files?q=${fileQuery}&fields=files(id, name, modifiedTime)`, {
            headers: { Authorization: `Bearer ${t}` }
        });
        const fileData = await fileRes.json();
        let fileId = (fileData.files && fileData.files.length > 0) ? fileData.files[0].id : null;

        let remoteJson = null;
        if (fileId) {
            const downloadRes = await fetch(`https://www.googleapis.com/drive/v3/files/${fileId}?alt=media`, {
                headers: { Authorization: `Bearer ${t}` }
            });
            if (downloadRes.ok) {
                remoteJson = await downloadRes.text();
            }
        }

        // 3. Fazer upload do arquivo atualizado
        if (fileId) {
            await fetch(`https://www.googleapis.com/upload/drive/v3/files/${fileId}?uploadType=media`, {
                method: 'PATCH',
                headers: {
                    Authorization: `Bearer ${t}`,
                    'Content-Type': 'application/json'
                },
                body: jsonContent
            });
        } else {
            const metadata = {
                name: 'manycontrol-sync.json',
                parents: [folderId],
                mimeType: 'application/json'
            };
            const boundary = '-------314159265358979323846';
            const delimiter = "\r\n--" + boundary + "\r\n";
            const close_delim = "\r\n--" + boundary + "--";

            const multipartRequestBody =
                delimiter +
                'Content-Type: application/json\r\n\r\n' +
                JSON.stringify(metadata) +
                delimiter +
                'Content-Type: application/json\r\n\r\n' +
                jsonContent +
                close_delim;

            await fetch('https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart', {
                method: 'POST',
                headers: {
                    Authorization: `Bearer ${t}`,
                    'Content-Type': 'multipart/related; boundary="' + boundary + '"'
                },
                body: multipartRequestBody
            });
        }

        return remoteJson;
    }
};
