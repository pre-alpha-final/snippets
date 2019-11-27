<?php
session_start();

$key = "foo";

if (isset($_GET["auth"])) {
    if ($_GET["auth"] != sha1($key . $_SESSION["rand"])) {
        echo "Wrong auth";
        exit();
    }
} else {
    if (isset($_SESSION["rand"]) === false) {
        $_SESSION["rand"] = sprintf('%04X%04X-%04X-%04X-%04X-%04X%04X%04X', mt_rand(0, 65535), mt_rand(0, 65535), mt_rand(0, 65535), mt_rand(16384, 20479), mt_rand(32768, 49151), mt_rand(0, 65535), mt_rand(0, 65535), mt_rand(0, 65535));
    }
    echo $_SESSION["rand"];
    exit();
}
?>

<html><body>content<body><html>