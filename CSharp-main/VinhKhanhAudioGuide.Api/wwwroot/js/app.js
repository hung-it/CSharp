/**
 * VinKhánh Audio Guide - Web Landing Page
 * Handles QR code scanning, audio playback, and app deep linking
 */

// ===================================
// Configuration
// ===================================

const CONFIG = {
    // Backend API URL - change this to match your backend server
    API_BASE_URL: 'http://localhost:5140/api/v1',
    
    // App URL scheme for deep linking
    APP_SCHEME: 'vkguid://',
    
    // Supported languages
    LANGUAGES: ['vi', 'en'],
    
    // Default language
    DEFAULT_LANGUAGE: 'vi'
};

// ===================================
// Translations
// ===================================

const translations = {
    vi: {
        loading: 'Đang tải...',
        errorNotFound: 'Không tìm thấy điểm này',
        errorMessage: 'Mã QR không hợp lệ hoặc điểm tham quan không tồn tại.',
        goHome: 'Về trang chủ',
        ready: 'Sẵn sàng',
        playing: 'Đang phát...',
        paused: 'Đã tạm dừng',
        completed: 'Đã phát xong!',
        error: 'Lỗi tải audio',
        viewMap: 'Xem trên bản đồ',
        getApp: 'Tải App để trải nghiệm tốt hơn',
        installMessage: 'Cài app VinhKhánh Audio Guide để nghe audio guide mọi lúc, mọi nơi!',
        downloadApp: 'Tải App',
        heroTitle: 'Audio Guide ẩm thực Vĩnh Khánh',
        heroSubtitle: 'Khám phá ẩm thực Phố Cổ Vĩnh Khánh qua audio guide tự động',
        enterCode: 'Nhập mã POI',
        search: 'Tìm kiếm',
        inputHint: 'Nhập mã trên mã QR hoặc sử dụng máy ảnh để quét',
        features: 'Tính năng',
        feature1Title: 'Audio Guide',
        feature1Desc: 'Nghe audio guide về ẩm thực địa phương',
        feature2Title: 'Bản đồ',
        feature2Desc: 'Khám phá các điểm ẩm thực trên bản đồ',
        feature3Title: 'Đa ngôn ngữ',
        feature3Desc: 'Hỗ trợ Tiếng Việt và Tiếng Anh',
        feature4Title: 'Premium',
        feature4Desc: 'Nâng cấp để truy cập nội dung độc quyền'
    },
    en: {
        loading: 'Loading...',
        errorNotFound: 'Point not found',
        errorMessage: 'Invalid QR code or the point does not exist.',
        goHome: 'Go Home',
        ready: 'Ready',
        playing: 'Playing...',
        paused: 'Paused',
        completed: 'Completed!',
        error: 'Audio load error',
        viewMap: 'View on map',
        getApp: 'Get the App for better experience',
        installMessage: 'Install VinhKhánh Audio Guide app to listen anytime, anywhere!',
        downloadApp: 'Download App',
        heroTitle: 'Vinh Khanh Culinary Audio Guide',
        heroSubtitle: 'Explore Old Quarter cuisine through automatic audio guide',
        enterCode: 'Enter POI Code',
        search: 'Search',
        inputHint: 'Enter the code on QR or use camera to scan',
        features: 'Features',
        feature1Title: 'Audio Guide',
        feature1Desc: 'Listen to audio guides about local cuisine',
        feature2Title: 'Map',
        feature2Desc: 'Explore culinary spots on the map',
        feature3Title: 'Multi-language',
        feature3Desc: 'Support Vietnamese and English',
        feature4Title: 'Premium',
        feature4Desc: 'Upgrade to access exclusive content'
    }
};

// ===================================
// State
// ===================================

const state = {
    currentLang: CONFIG.DEFAULT_LANGUAGE,
    poiCode: null,
    poiData: null,
    audioUrl: null,
    audioPlayer: null,
    isPlaying: false,
    currentAudioFile: null
};

// ===================================
// DOM Elements
// ===================================

const elements = {
    // Sections
    loadingState: document.getElementById('loadingState'),
    mainContent: document.getElementById('mainContent'),
    homeSection: document.getElementById('homeSection'),
    poiSection: document.getElementById('poiSection'),
    errorState: document.getElementById('errorState'),
    
    // POI Info
    poiDistrict: document.getElementById('poiDistrict'),
    poiName: document.getElementById('poiName'),
    poiCode: document.getElementById('poiCode'),
    poiDescription: document.getElementById('poiDescription'),
    poiImage: document.getElementById('poiImage'),
    poiImageContainer: document.getElementById('poiImageContainer'),
    imagePlaceholder: document.getElementById('imagePlaceholder'),
    mapLink: document.getElementById('mapLink'),
    
    // Audio Player
    playerIcon: document.getElementById('playerIcon'),
    playerStatus: document.getElementById('playerStatus'),
    progressFill: document.getElementById('progressFill'),
    currentTime: document.getElementById('currentTime'),
    totalTime: document.getElementById('totalTime'),
    playBtn: document.getElementById('playBtn'),
    playIcon: document.getElementById('playIcon'),
    rewindBtn: document.getElementById('rewindBtn'),
    forwardBtn: document.getElementById('forwardBtn'),
    langViBtn: document.getElementById('langViBtn'),
    langEnBtn: document.getElementById('langEnBtn'),
    
    // Home
    poiCodeInput: document.getElementById('poiCodeInput'),
    submitBtn: document.getElementById('submitBtn'),
    
    // Install
    installPrompt: document.getElementById('installPrompt'),
    installLink: document.getElementById('installLink'),
    
    // Header
    langToggle: document.getElementById('langToggle'),
    currentLang: document.getElementById('currentLang')
};

// ===================================
// Utility Functions
// ===================================

function formatTime(seconds) {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
}

function translate(key) {
    return translations[state.currentLang]?.[key] || translations['vi'][key] || key;
}

function updateTranslations() {
    document.querySelectorAll('[data-i18n]').forEach(el => {
        const key = el.getAttribute('data-i18n');
        el.textContent = translate(key);
    });
    
    // Update lang display
    if (elements.currentLang) {
        elements.currentLang.textContent = state.currentLang.toUpperCase();
    }
    
    // Update language buttons
    if (elements.langViBtn) {
        elements.langViBtn.classList.toggle('active', state.currentLang === 'vi');
    }
    if (elements.langEnBtn) {
        elements.langEnBtn.classList.toggle('active', state.currentLang === 'en');
    }
}

function showSection(sectionId) {
    // Hide all sections
    elements.loadingState.style.display = 'none';
    elements.homeSection.style.display = 'none';
    elements.poiSection.style.display = 'none';
    elements.errorState.style.display = 'none';
    elements.mainContent.style.display = 'none';
    
    // Show requested section
    switch (sectionId) {
        case 'loading':
            elements.loadingState.style.display = 'flex';
            break;
        case 'home':
            elements.homeSection.style.display = 'flex';
            elements.mainContent.style.display = 'block';
            break;
        case 'poi':
            elements.poiSection.style.display = 'flex';
            elements.mainContent.style.display = 'block';
            break;
        case 'error':
            elements.errorState.style.display = 'block';
            elements.mainContent.style.display = 'block';
            break;
    }
}

function parseQRPayload(payload) {
    if (!payload) return null;
    
    payload = payload.trim();
    
    // Handle "QR:" prefix
    if (payload.startsWith('QR:')) {
        return payload.substring(3).trim();
    }
    
    // Handle "vk://poi/" prefix
    if (payload.startsWith('vk://poi/')) {
        return payload.substring('vk://poi/'.length).trim();
    }
    
    // Handle "vk://poi" prefix
    if (payload.startsWith('vk://poi')) {
        return payload.replace('vk://poi', '').replace(/\//g, '').trim();
    }
    
    // Return as-is if no known prefix
    return payload;
}

// ===================================
// API Functions
// ===================================

async function fetchPOI(poiCode) {
    try {
        // First, try to get POI list and find matching code
        const response = await fetch(`${CONFIG.API_BASE_URL}/pois`);
        
        if (!response.ok) {
            throw new Error('Failed to fetch POIs');
        }
        
        const pois = await response.json();
        
        // Find POI by code
        const poi = pois.find(p => 
            p.code.toLowerCase() === poiCode.toLowerCase()
        );
        
        if (!poi) {
            return null;
        }
        
        // Get full POI details with audio
        const detailResponse = await fetch(`${CONFIG.API_BASE_URL}/pois/${poi.id}`);
        
        if (!detailResponse.ok) {
            throw new Error('Failed to fetch POI details');
        }
        
        return await detailResponse.json();
    } catch (error) {
        console.error('Error fetching POI:', error);
        throw error;
    }
}

async function startQRSession(userId, qrPayload, languageCode) {
    try {
        const response = await fetch(`${CONFIG.API_BASE_URL}/qr/start`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                userId: userId,
                qrPayload: qrPayload,
                languageCode: languageCode
            })
        });
        
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Failed to start QR session');
        }
        
        return await response.json();
    } catch (error) {
        console.error('Error starting QR session:', error);
        throw error;
    }
}

async function getOrCreateAnonymousUser() {
    try {
        const response = await fetch(`${CONFIG.API_BASE_URL}/users/anonymous`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        
        if (!response.ok) {
            throw new Error('Failed to create anonymous user');
        }
        
        return await response.json();
    } catch (error) {
        console.error('Error creating anonymous user:', error);
        throw error;
    }
}

// ===================================
// Audio Player
// ===================================

function initAudioPlayer(audioUrl) {
    // Clean up existing player
    if (state.audioPlayer) {
        state.audioPlayer.pause();
        state.audioPlayer = null;
    }
    
    // Create new audio element
    state.audioPlayer = new Audio();
    state.audioPlayer.crossOrigin = 'anonymous';
    
    // Set up event listeners
    state.audioPlayer.addEventListener('loadedmetadata', () => {
        elements.totalTime.textContent = formatTime(state.audioPlayer.duration);
        state.isPlaying = false;
        updatePlayButton();
    });
    
    state.audioPlayer.addEventListener('timeupdate', () => {
        if (state.audioPlayer.duration > 0) {
            const progress = (state.audioPlayer.currentTime / state.audioPlayer.duration) * 100;
            elements.progressFill.style.width = `${progress}%`;
            elements.currentTime.textContent = formatTime(state.audioPlayer.currentTime);
        }
    });
    
    state.audioPlayer.addEventListener('ended', () => {
        state.isPlaying = false;
        elements.progressFill.style.width = '0%';
        elements.currentTime.textContent = '0:00';
        updatePlayButton();
        setPlayerStatus(translate('completed'));
        elements.playerIcon.textContent = '✅';
    });
    
    state.audioPlayer.addEventListener('error', (e) => {
        console.error('Audio error:', e);
        setPlayerStatus(translate('error'));
        elements.playerIcon.textContent = '❌';
        state.isPlaying = false;
        updatePlayButton();
    });
    
    // Set audio source
    state.audioUrl = audioUrl;
    state.audioPlayer.src = audioUrl;
    
    setPlayerStatus(translate('ready'));
    elements.totalTime.textContent = '0:00';
    elements.currentTime.textContent = '0:00';
    elements.progressFill.style.width = '0%';
}

function togglePlay() {
    if (!state.audioPlayer || !state.audioUrl) {
        return;
    }
    
    if (state.isPlaying) {
        state.audioPlayer.pause();
        state.isPlaying = false;
    } else {
        state.audioPlayer.play().then(() => {
            state.isPlaying = true;
        }).catch(error => {
            console.error('Play error:', error);
            setPlayerStatus(translate('error'));
        });
    }
    
    updatePlayButton();
}

function updatePlayButton() {
    if (state.isPlaying) {
        elements.playIcon.textContent = '⏸️';
        elements.playBtn.classList.add('playing');
        elements.playerIcon.textContent = '🎵';
        setPlayerStatus(translate('playing'));
        document.querySelector('.audio-player-card')?.classList.add('playing');
    } else {
        elements.playIcon.textContent = '▶️';
        elements.playBtn.classList.remove('playing');
        if (elements.playerIcon.textContent === '🎵') {
            elements.playerIcon.textContent = '🎧';
        }
        document.querySelector('.audio-player-card')?.classList.remove('playing');
    }
}

function setPlayerStatus(status) {
    elements.playerStatus.textContent = status;
}

function rewindAudio() {
    if (state.audioPlayer) {
        state.audioPlayer.currentTime = Math.max(0, state.audioPlayer.currentTime - 10);
    }
}

function forwardAudio() {
    if (state.audioPlayer) {
        state.audioPlayer.currentTime = Math.min(
            state.audioPlayer.duration,
            state.audioPlayer.currentTime + 10
        );
    }
}

// ===================================
// App Deep Link
// ===================================

function tryOpenApp() {
    const appUrl = `${CONFIG.APP_SCHEME}poi/${state.poiCode}`;
    
    // Try to open app
    window.location.href = appUrl;
    
    // If app is not installed, this will fail and we stay on web
    // Set a fallback timer to show install prompt
    setTimeout(() => {
        // Check if we were redirected back (app not installed)
        if (document.visibilityState === 'visible') {
            showInstallPrompt();
        }
    }, 2000);
}

function showInstallPrompt() {
    if (elements.installPrompt) {
        elements.installPrompt.style.display = 'block';
    }
}

// ===================================
// UI Functions
// ===================================

function displayPOI(poi) {
    state.poiData = poi;
    
    // Update POI info
    elements.poiDistrict.textContent = poi.district || 'Vĩnh Khánh';
    elements.poiName.textContent = poi.name || 'Điểm tham quan';
    elements.poiCode.textContent = poi.code || '';
    elements.poiDescription.textContent = poi.description || 'Không có mô tả';
    
    // Update image
    if (poi.imageUrl) {
        elements.poiImage.src = poi.imageUrl;
        elements.poiImage.style.display = 'block';
        elements.imagePlaceholder.style.display = 'none';
    } else {
        elements.poiImage.style.display = 'none';
        elements.imagePlaceholder.style.display = 'flex';
    }
    
    // Update map link
    if (poi.mapLink) {
        elements.mapLink.href = poi.mapLink;
        elements.mapLink.style.display = 'inline-flex';
    } else if (poi.latitude && poi.longitude) {
        elements.mapLink.href = `https://maps.google.com/?q=${poi.latitude},${poi.longitude}`;
        elements.mapLink.style.display = 'inline-flex';
    } else {
        elements.mapLink.style.display = 'none';
    }
    
    // Get audio for current language
    const audioAssets = poi.audioAssets || [];
    let audioAsset = audioAssets.find(a => a.languageCode === state.currentLang);
    
    // Fallback to Vietnamese
    if (!audioAsset) {
        audioAsset = audioAssets.find(a => a.languageCode === 'vi');
    }
    
    // Fallback to any available audio
    if (!audioAsset && audioAssets.length > 0) {
        audioAsset = audioAssets[0];
    }
    
    if (audioAsset && audioAsset.filePath) {
        // Build full audio URL
        const audioPath = audioAsset.filePath;
        let audioUrl;
        
        if (audioPath.startsWith('http://') || audioPath.startsWith('https://')) {
            audioUrl = audioPath;
        } else {
            // Build from base URL
            const baseUrl = CONFIG.API_BASE_URL.replace('/api/v1', '');
            audioUrl = `${baseUrl}/media/audio/${audioPath.replace('/media/audio/', '')}`;
        }
        
        initAudioPlayer(audioUrl);
    } else {
        // No audio available
        setPlayerStatus('Chưa có audio');
        elements.playBtn.disabled = true;
    }
    
    // Update meta tags for social sharing
    updateMetaTags(poi);
    
    // Show POI section
    showSection('poi');
}

function displayError(message) {
    if (message) {
        const errorMessageEl = elements.errorState.querySelector('p');
        if (errorMessageEl) {
            errorMessageEl.textContent = message;
        }
    }
    showSection('error');
}

function updateMetaTags(poi) {
    // Update Open Graph tags
    const ogTitle = document.querySelector('meta[property="og:title"]');
    const ogDesc = document.querySelector('meta[property="og:description"]');
    const ogImage = document.querySelector('meta[property="og:image"]');
    
    if (ogTitle) ogTitle.content = `${poi.name} - VinKhánh Audio Guide`;
    if (ogDesc) ogDesc.content = poi.description || 'Audio Guide cho ẩm thực Vĩnh Khánh';
    if (ogImage && poi.imageUrl) ogImage.content = poi.imageUrl;
    
    // Update app link meta tags
    const iosUrl = document.querySelector('meta[property="al:ios:url"]');
    const androidUrl = document.querySelector('meta[property="al:android:url"]');
    
    if (iosUrl) iosUrl.content = `${CONFIG.APP_SCHEME}poi/${poi.code}`;
    if (androidUrl) androidUrl.content = `${CONFIG.APP_SCHEME}poi/${poi.code}`;
    
    // Update page title
    document.title = `${poi.name} - VinKhánh Audio Guide`;
}

// ===================================
// Main Functions
// ===================================

async function loadPOI(poiCode) {
    showSection('loading');
    
    try {
        // Try to get POI data
        const poi = await fetchPOI(poiCode);
        
        if (!poi) {
            displayError(translate('errorNotFound'));
            return;
        }
        
        state.poiCode = poiCode;
        displayPOI(poi);
        
        // Try to open app automatically
        // Disabled for web-only mode
        // tryOpenApp();
        
    } catch (error) {
        console.error('Error loading POI:', error);
        displayError(error.message || translate('errorMessage'));
    }
}

async function loadFromURL() {
    // Check URL params
    const urlParams = new URLSearchParams(window.location.search);
    const poiCode = urlParams.get('poi') || urlParams.get('code');
    
    if (poiCode) {
        // Parse the QR payload to extract POI code
        const parsedCode = parseQRPayload(poiCode);
        await loadPOI(parsedCode);
    } else {
        // Show home section
        showSection('home');
    }
}

// ===================================
// Event Listeners
// ===================================

function initEventListeners() {
    // Play/Pause button
    elements.playBtn.addEventListener('click', togglePlay);
    
    // Rewind/Forward buttons
    elements.rewindBtn.addEventListener('click', rewindAudio);
    elements.forwardBtn.addEventListener('click', forwardAudio);
    
    // Language buttons
    elements.langViBtn.addEventListener('click', async () => {
        if (state.currentLang !== 'vi') {
            state.currentLang = 'vi';
            updateTranslations();
            
            if (state.poiData) {
                await loadPOI(state.poiData.code);
            }
        }
    });
    
    elements.langEnBtn.addEventListener('click', async () => {
        if (state.currentLang !== 'en') {
            state.currentLang = 'en';
            updateTranslations();
            
            if (state.poiData) {
                await loadPOI(state.poiData.code);
            }
        }
    });
    
    // Language toggle in header
    elements.langToggle.addEventListener('click', () => {
        state.currentLang = state.currentLang === 'vi' ? 'en' : 'vi';
        updateTranslations();
        
        if (state.poiData) {
            loadPOI(state.poiData.code);
        }
    });
    
    // Home search
    elements.submitBtn.addEventListener('click', () => {
        const code = elements.poiCodeInput.value.trim();
        if (code) {
            const parsedCode = parseQRPayload(code);
            window.location.href = `/?poi=${encodeURIComponent(parsedCode)}`;
        }
    });
    
    elements.poiCodeInput.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') {
            elements.submitBtn.click();
        }
    });
    
    // Keyboard shortcuts for audio player
    document.addEventListener('keydown', (e) => {
        if (state.poiData && elements.poiSection.style.display !== 'none') {
            switch (e.key) {
                case ' ':
                    e.preventDefault();
                    togglePlay();
                    break;
                case 'ArrowLeft':
                    rewindAudio();
                    break;
                case 'ArrowRight':
                    forwardAudio();
                    break;
            }
        }
    });
}

// ===================================
// Initialization
// ===================================

function init() {
    // Set initial language from browser or default
    const browserLang = navigator.language.toLowerCase();
    if (browserLang.startsWith('en')) {
        state.currentLang = 'en';
    }
    
    // Update translations
    updateTranslations();
    
    // Initialize event listeners
    initEventListeners();
    
    // Load POI from URL or show home
    loadFromURL();
}

// Start the app
document.addEventListener('DOMContentLoaded', init);

// Also handle case where DOM is already loaded
if (document.readyState !== 'loading') {
    init();
}
