// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// ==========================================================
// RECORDAR ESTADO DE LOS GRUPOS DEL SIDEBAR (ADMIN)
// ==========================================================

document.addEventListener("DOMContentLoaded", function () {

    const grupos = document.querySelectorAll(".admin-menu-group");

    grupos.forEach(function (grupo) {

        const clave = "sidebar-" + grupo.id;

        // ==========================================
        // RESTAURAR ESTADO GUARDADO
        // ==========================================

        const estadoGuardado = localStorage.getItem(clave);

        if (estadoGuardado === "abierto") {
            grupo.setAttribute("open", "");
        } else if (estadoGuardado === "cerrado") {
            grupo.removeAttribute("open");
        } else {
            // Primera vez: por defecto abierto
            grupo.setAttribute("open");
        }


        // ==========================================
        // GUARDAR CADA VEZ QUE EL USUARIO LO CAMBIE
        // ==========================================

        grupo.addEventListener("toggle", function () {

            localStorage.setItem(
                clave,
                grupo.open ? "abierto" : "cerrado"
            );

        });

    });

});