// Copyright 2015 Toggl Desktop developers

#include "urls.h"

namespace toggl {

namespace urls {

// Whether requests are sent to staging backend

#ifndef TOGGL_PRODUCTION_BUILD
static bool use_staging_as_backend = true;
#else
static bool use_staging_as_backend = false;
#endif

// Whether requests are allowed to Toggl backend
static bool im_a_teapot_ = false;

// Whether requests are allowed at all (like in tests)
static bool requests_allowed_ = true;

void SetUseStagingAsBackend(const bool value) {
    use_staging_as_backend = value;
}

bool IsUsingStagingAsBackend() {
    return use_staging_as_backend;
}

std::string Main() {
    if (use_staging_as_backend) {
        return "https://track.toggl.space";
    }
    return "https://api.track.toggl.com";
}

std::string API() {
    if (use_staging_as_backend) {
        return "https://desktop.track.toggl.space";
    }
    return "https://api.track.toggl.com";
}

std::string SyncAPI() {
    if (use_staging_as_backend) {
        return "https://sync.toggl.space/";
    }
    return "https://api.track.toggl.com/";
}

std::string TimelineUpload() {
    if (use_staging_as_backend) {
        return "https://desktop.track.toggl.space";
    }
    return "https://api.track.toggl.com";
}

std::string WebSocket() {
    if (use_staging_as_backend) {
        return "https://desktop.track.toggl.space";
    }
    return "https://api.track.toggl.com";
}

bool ImATeapot() {
    return im_a_teapot_;
}

void SetImATeapot(const bool value) {
    im_a_teapot_ = value;
}

bool RequestsAllowed() {
    return requests_allowed_;
}

void SetRequestsAllowed(const bool value) {
    requests_allowed_ = value;
}


}  // namespace urls

}  // namespace toggl
