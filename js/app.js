let tokenClient = null;
let googleAccessToken = localStorage.getItem('manycontrol_google_token') || null;

window.manyControlJs = {
    downloadFile: async function (filename, content, mimeType) {
        const type = mimeType || 'application/json;charset=utf-8;';
        const blob = new Blob([content], { type: type });

        // Tenta usar a Web Share API (iOS 15+, iPadOS, Android) permitindo "Salvar em Arquivos", AirDrop, etc.
        if (navigator.canShare && typeof File !== 'undefined') {
            try {
                const file = new File([blob], filename, { type: type });
                if (navigator.canShare({ files: [file] })) {
                    await navigator.share({
                        files: [file],
                        title: filename
                    });
                    return true;
                }
            } catch (err) {
                if (err.name === 'AbortError') return true;
                console.warn('navigator.share falhou ou foi cancelado, usando download padrão:', err);
            }
        }

        // Fallback clássico para download em computadores (Chrome, Firefox, Edge)
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        a.rel = 'noopener';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);

        // Posterga a revogação para não abortar o download em navegadores móveis
        setTimeout(() => {
            URL.revokeObjectURL(url);
        }, 2500);
        return true;
    },

    triggerFileInput: function (elementId) {
        const elem = document.getElementById(elementId);
        if (elem) {
            elem.value = '';
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
            callback: async (tokenResponse) => {
                if (tokenResponse && tokenResponse.error) {
                    console.error('Google OAuth Error:', tokenResponse);
                    alert('Erro na autorização do Google: ' + (tokenResponse.error_description || tokenResponse.error));
                    return;
                }
                if (tokenResponse && tokenResponse.access_token) {
                    googleAccessToken = tokenResponse.access_token;
                    localStorage.setItem('manycontrol_google_token', googleAccessToken);
                    const expiresIn = tokenResponse.expires_in ? parseInt(tokenResponse.expires_in, 10) : 3600;
                    const expiry = Date.now() + (expiresIn * 1000);
                    localStorage.setItem('manycontrol_google_token_expiry', expiry.toString());

                    // Busca o e-mail do usuário conectado
                    await window.manyControlGoogleDrive.getUserEmail(googleAccessToken);

                    if (window._googleDotNetRef) {
                        try {
                            await window._googleDotNetRef.invokeMethodAsync('OnGoogleAuthSuccess', googleAccessToken);
                        } catch (err) {
                            console.error('Erro ao invocar OnGoogleAuthSuccess:', err);
                        }
                    }
                }
            },
            error_callback: (nonOAuthError) => {
                console.error('Google Identity Services Non-OAuth Error:', nonOAuthError);
                if (nonOAuthError && (nonOAuthError.type === 'popup_failed_to_open' || nonOAuthError.type === 'popup_closed')) {
                    alert('A janela de login do Google não pôde ser aberta. Verifique se o navegador bloqueou pop-ups para este site.');
                } else {
                    alert('Falha ao abrir login do Google: ' + (nonOAuthError?.message || nonOAuthError?.type || 'Verifique bloqueadores de anúncios ou pop-ups.'));
                }
            }
        });
    },

    isTokenValid: function() {
        const token = localStorage.getItem('manycontrol_google_token');
        if (!token) return false;
        const expiry = localStorage.getItem('manycontrol_google_token_expiry');
        if (expiry && Date.now() > parseInt(expiry, 10)) {
            return false;
        }
        return true;
    },

    requestToken: function(dotNetRef, clientId) {
        if (dotNetRef) {
            window._googleDotNetRef = dotNetRef;
        }
        const cid = clientId || '467782905209-n8n3i4thm5ga7bphtbqqk7jqq1karjbl.apps.googleusercontent.com';
        if (!tokenClient && cid) {
            this.init(cid);
        }
        if (tokenClient) {
            tokenClient.requestAccessToken({ prompt: 'select_account' });
        } else if (window.google && window.google.accounts && window.google.accounts.oauth2) {
            this.init(cid);
            if (tokenClient) {
                tokenClient.requestAccessToken({ prompt: 'select_account' });
            }
        } else {
            alert('Aguarde o carregamento do serviço do Google ou verifique se bloqueadores de pop-up / rastreadores estão ativos.');
        }
    },

    disconnect: function() {
        if (googleAccessToken && window.google && window.google.accounts && window.google.accounts.oauth2) {
            try {
                window.google.accounts.oauth2.revoke(googleAccessToken, () => {});
            } catch (e) {
                console.warn('Erro ao revogar token:', e);
            }
        }
        googleAccessToken = null;
        localStorage.removeItem('manycontrol_google_token');
        localStorage.removeItem('manycontrol_google_email');
        localStorage.removeItem('manycontrol_google_token_expiry');
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

    getOrCreateFolder: async function(t) {
        const folderQuery = encodeURIComponent("name = 'ManyControl' and mimeType = 'application/vnd.google-apps.folder' and trashed = false");
        let folderRes = await fetch(`https://www.googleapis.com/drive/v3/files?q=${folderQuery}&fields=files(id, name)`, {
            headers: { Authorization: `Bearer ${t}` }
        });

        if (folderRes.status === 401) {
            this.disconnect();
            if (window._googleDotNetRef) {
                try { window._googleDotNetRef.invokeMethodAsync('OnGoogleSessionExpired'); } catch (e) {}
            }
            throw new Error('Sessão expirada. Por favor, conecte-se novamente com o Google.');
        }

        let folderData = await folderRes.json();
        if (folderData.files && folderData.files.length > 0) {
            return folderData.files[0].id;
        }

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

        if (createFolderRes.status === 401) {
            this.disconnect();
            if (window._googleDotNetRef) {
                try { window._googleDotNetRef.invokeMethodAsync('OnGoogleSessionExpired'); } catch (e) {}
            }
            throw new Error('Sessão expirada. Por favor, conecte-se novamente com o Google.');
        }

        const newFolder = await createFolderRes.json();
        return newFolder.id;
    },

    downloadDriveFile: async function() {
        if (!this.isTokenValid()) {
            this.disconnect();
            if (window._googleDotNetRef) {
                try { window._googleDotNetRef.invokeMethodAsync('OnGoogleSessionExpired'); } catch (e) {}
            }
            throw new Error('Sessão expirada. Por favor, conecte-se novamente com o Google.');
        }

        const t = googleAccessToken || localStorage.getItem('manycontrol_google_token');
        if (!t) throw new Error('Não autenticado com o Google Drive.');

        const folderId = await this.getOrCreateFolder(t);
        if (!folderId) return null;

        const fileQuery = encodeURIComponent(`name = 'manycontrol-sync.json' and '${folderId}' in parents and trashed = false`);
        const fileRes = await fetch(`https://www.googleapis.com/drive/v3/files?q=${fileQuery}&fields=files(id, name, modifiedTime)`, {
            headers: { Authorization: `Bearer ${t}` }
        });

        if (fileRes.status === 401) {
            this.disconnect();
            if (window._googleDotNetRef) {
                try { window._googleDotNetRef.invokeMethodAsync('OnGoogleSessionExpired'); } catch (e) {}
            }
            throw new Error('Sessão expirada. Por favor, conecte-se novamente com o Google.');
        }

        const fileData = await fileRes.json();
        const fileId = (fileData.files && fileData.files.length > 0) ? fileData.files[0].id : null;

        if (!fileId) return null;

        const downloadRes = await fetch(`https://www.googleapis.com/drive/v3/files/${fileId}?alt=media`, {
            headers: { Authorization: `Bearer ${t}` }
        });

        if (downloadRes.status === 401) {
            this.disconnect();
            if (window._googleDotNetRef) {
                try { window._googleDotNetRef.invokeMethodAsync('OnGoogleSessionExpired'); } catch (e) {}
            }
            throw new Error('Sessão expirada. Por favor, conecte-se novamente com o Google.');
        }

        if (downloadRes.ok) {
            return await downloadRes.text();
        }
        return null;
    },

    uploadDriveFile: async function(jsonContent) {
        if (!this.isTokenValid()) {
            this.disconnect();
            if (window._googleDotNetRef) {
                try { window._googleDotNetRef.invokeMethodAsync('OnGoogleSessionExpired'); } catch (e) {}
            }
            throw new Error('Sessão expirada. Por favor, conecte-se novamente com o Google.');
        }

        const t = googleAccessToken || localStorage.getItem('manycontrol_google_token');
        if (!t) throw new Error('Não autenticado com o Google Drive.');

        const folderId = await this.getOrCreateFolder(t);
        if (!folderId) throw new Error('Falha ao obter pasta ManyControl no Google Drive.');

        const fileQuery = encodeURIComponent(`name = 'manycontrol-sync.json' and '${folderId}' in parents and trashed = false`);
        const fileRes = await fetch(`https://www.googleapis.com/drive/v3/files?q=${fileQuery}&fields=files(id, name)`, {
            headers: { Authorization: `Bearer ${t}` }
        });

        if (fileRes.status === 401) {
            this.disconnect();
            if (window._googleDotNetRef) {
                try { window._googleDotNetRef.invokeMethodAsync('OnGoogleSessionExpired'); } catch (e) {}
            }
            throw new Error('Sessão expirada. Por favor, conecte-se novamente com o Google.');
        }

        const fileData = await fileRes.json();
        const fileId = (fileData.files && fileData.files.length > 0) ? fileData.files[0].id : null;

        if (fileId) {
            const patchRes = await fetch(`https://www.googleapis.com/upload/drive/v3/files/${fileId}?uploadType=media`, {
                method: 'PATCH',
                headers: {
                    Authorization: `Bearer ${t}`,
                    'Content-Type': 'application/json'
                },
                body: jsonContent
            });
            if (patchRes.status === 401) {
                this.disconnect();
                if (window._googleDotNetRef) {
                    try { window._googleDotNetRef.invokeMethodAsync('OnGoogleSessionExpired'); } catch (e) {}
                }
                throw new Error('Sessão expirada. Por favor, conecte-se novamente com o Google.');
            }
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

            const postRes = await fetch('https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart', {
                method: 'POST',
                headers: {
                    Authorization: `Bearer ${t}`,
                    'Content-Type': 'multipart/related; boundary="' + boundary + '"'
                },
                body: multipartRequestBody
            });
            if (postRes.status === 401) {
                this.disconnect();
                if (window._googleDotNetRef) {
                    try { window._googleDotNetRef.invokeMethodAsync('OnGoogleSessionExpired'); } catch (e) {}
                }
                throw new Error('Sessão expirada. Por favor, conecte-se novamente com o Google.');
            }
        }
    },

    syncDrive: async function(jsonContent) {
        const remote = await this.downloadDriveFile();
        if (jsonContent) {
            await this.uploadDriveFile(jsonContent);
        }
        return remote;
    }
};
