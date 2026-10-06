// Konfigurasi API Backend
window.API_BASE_URL = window.API_BASE_URL || '/api';
var API_BASE_URL = window.API_BASE_URL;

// Helper Session Pengguna & JWT Token
window.AuthSession = window.AuthSession || {};
var AuthSession = window.AuthSession;

// Cookie Helper
AuthSession.getCookie = function (name) {
    var value = document.cookie.split("; ").find(function (row) {
        return row.startsWith(name + "=");
    });
    return value ? decodeURIComponent(value.split("=")[1]) : null;
};

AuthSession.setCookie = function (name, value, hours) {
    var date = new Date();
    date.setTime(date.getTime() + (hours || 24) * 60 * 60 * 1000);
    document.cookie = name + "=" + encodeURIComponent(value) + ";expires=" + date.toUTCString() + ";path=/;SameSite=Lax";
};

AuthSession.removeCookie = function (name) {
    document.cookie = name + "=;expires=Thu, 01 Jan 1970 00:00:00 UTC;path=/;";
};

// Token Management
AuthSession.getToken = function () {
    var cookieToken = this.getCookie('jwtToken');
    if (!cookieToken) {
        try {
            localStorage.removeItem('jwtToken');
            localStorage.removeItem('currentUser');
        } catch (e) {}
        return null;
    }
    return localStorage.getItem('jwtToken') || cookieToken;
};

AuthSession.setToken = function (token) {
    if (!token) return;
    localStorage.setItem('jwtToken', token);
    this.setCookie('jwtToken', token, 24);
};

AuthSession.removeToken = function () {
    localStorage.removeItem('jwtToken');
    this.removeCookie('jwtToken');
};

AuthSession.isTokenExpired = function (token) {
    if (!token) return true;
    try {
        var payload = JSON.parse(atob(token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")));
        return payload.exp < Date.now() / 1000;
    } catch (e) {
        return true;
    }
};

// User Management
AuthSession.getUser = function () {
    try {
        var data = localStorage.getItem('currentUser');
        return data ? JSON.parse(data) : null;
    } catch (e) {
        return null;
    }
};

AuthSession.setUser = function (user) {
    localStorage.setItem('currentUser', JSON.stringify(user));
    if (user && user.namaLengkap) {
        this.setCookie('userName', user.namaLengkap, 24);
    }
    if (user && user.roleName) {
        this.setCookie('userRole', user.roleName, 24);
    }
};

AuthSession.removeUser = function () {
    localStorage.removeItem('currentUser');
    this.removeToken();
    this.removeCookie('userName');
    this.removeCookie('userRole');
};

AuthSession.isAuthenticated = function () {
    var token = this.getToken();
    var user = this.getUser();
    if (!token || !user) return false;
    return !this.isTokenExpired(token);
};

AuthSession.requireAuth = function () {
    if (!this.isAuthenticated()) {
        this.removeUser();
        window.location.replace('/Account/Login');
    }
};
