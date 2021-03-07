$(function () {

    var noteids = [];

    $("div[data-note-id]").each(function (i, e) {
        noteids.push($(e).data("note-id"));
    });

    $.ajax({
        method: "POST",
        url: "/Note/GetLiked",
        data: { ids: noteids }
    }).done(function (data) {

        if (data.result != null && data.result.length > 0) {
            for (var i = 0; i < data.result.length; i++) {
                var id = data.result[i];
                var likedNote = $("div[data-note-id=" + id + "]");
                var btn = likedNote.find("button[data-liked]");
                var span = btn.find("span.like");

                btn.data("liked", true);

                span.removeClass("fa fa-heart y");
                span.addClass("fa fa-heart x");
                $(".x").css({ "color": "red" });
            }

        }

    }).fail(function () {

    });



    $("button[data-liked]").click(function () {
        var btn = $(this);
        var liked = btn.data("liked");
        var noteid = btn.data("note-id");
        var spanStar = btn.find("span.like");
        var spanCount = btn.find("span.like-count");

        console.log(liked);
        console.log("like count : " + spanCount.text());

        $.ajax({
            method: "POST",
            url: "/Note/SetLikeState",
            data: { "noteid": noteid, "liked": !liked }
        }).done(function (data) {

            console.log(data);

            if (data.hasError) {
                alert(data.errorMessage);
            } else {
                liked = !liked;
                btn.data("liked", liked);
                spanCount.text(data.result);

                console.log("like count(after) : " + spanCount.text());


                spanStar.removeClass("fa fa-heart y");
                spanStar.removeClass("fa fa-heart x");


                if (liked) {
                    spanStar.addClass("fa fa-heart x");
                    $(".x").css({ "color": "red" });
                } else {
                    spanStar.addClass("fa fa-heart y");
                    $(".y").css({ "color": "black" });

                }

            }

        }).fail(function () {
            alert("Sunucu ile bağlantı kurulamadı.");
        })

    });
});