// Copyright 2014 Toggl Desktop developers.

#ifndef SRC_CONTEXT_H_
#define SRC_CONTEXT_H_

#include <string>
#include <vector>
#include <map>
#include <set>
#include <memory>
#include <iostream> // NOLINT

#include "analytics.h"
#include "util/custom_error_handler.h"
#include "feedback.h"
#include "gui.h"
#include "help_article.h"
#include "idle.h"
#include "util/logger.h"
#include "model_change.h"
#include "model/timeline_event.h"
#include "timeline_notifications.h"
#include "types.h"
#include "websocket_client.h"
#include "model/alpha_features.h"

#include <Poco/Activity.h>
#include <Poco/LocalDateTime.h>
#include <Poco/Timestamp.h>
#include <Poco/Util/Timer.h>

#ifdef TOGGL_ALLOW_UPDATE_CHECK
# define UPDATE_CHECK_DISABLED false
#else
# define UPDATE_CHECK_DISABLED true
#endif

#if defined(TOGGL_PRODUCTION_BUILD) && !defined(APP_ENVIRONMENT)
# define APP_ENVIRONMENT "production"
#elif !defined(APP_ENVIRONMENT)
# define APP_ENVIRONMENT "development"
#endif

namespace toggl {

class Database;
class TimelineUploader;
class WindowChangeRecorder;
class OnboardingService;

class TOGGL_INTERNAL_EXPORT UIElements {
 public:
    UIElements()
        : first_load(false)
    , display_time_entries(false)
    , display_time_entry_autocomplete(false)
    , display_mini_timer_autocomplete(false)
    , display_project_autocomplete(false)
    , display_client_select(false)
    , display_workspace_select(false)
    , display_timer_state(false)
    , display_time_entry_editor(false)
    , open_settings(false)
    , open_time_entry_list(false)
    , open_time_entry_editor(false)
    , display_autotracker_rules(false)
    , display_settings(false)
    , time_entry_editor_guid("")
    , time_entry_editor_field("")
    , display_unsynced_items(false)
    , display_timeline(false)
    , open_timeline(false) {}

    static UIElements Reset();

    std::string String() const;

    void ApplyChanges(
        const std::string &editor_guid,
        const std::vector<ModelChange> &changes);

    bool first_load;
    bool display_time_entries;
    bool display_time_entry_autocomplete;
    bool display_mini_timer_autocomplete;
    bool display_project_autocomplete;
    bool display_client_select;
    bool display_workspace_select;
    bool display_timer_state;
    bool display_time_entry_editor;
    bool open_settings;
    bool open_time_entry_list;
    bool open_time_entry_editor;
    bool display_autotracker_rules;
    bool display_settings;
    std::string time_entry_editor_guid;
    std::string time_entry_editor_field;
    bool display_unsynced_items;
    bool display_timeline;
    bool open_timeline;
};

class TOGGL_INTERNAL_EXPORT Context : public TimelineDatasource {
 public:
    Context(
        const std::string &app_name,
        const std::string &app_version);
    virtual ~Context();

    GUI *UI() {
        return &ui_;
    }

    // Check for logged in user etc, start up the app
    error StartEvents();

    // Close connections and wait for tasks to finish
    void Shutdown();

    void FullSync();
    void Sync();
    void TimelineUpdateServerSettings();
    error SendFeedback(const Feedback &);

    // Load model update from JSON string (from WebSocket)
    error LoadUpdateFromJSONString(const std::string &json);

    void SetWebSocketClientURL(const std::string &value);

    error SetDBPath(const std::string &path);

    void SetUpdatePath(const std::string &path) {
        update_path_ = path;
    }
    const std::string &UpdatePath() {
        return update_path_;
    }

    void SetEnvironment(const std::string &environment);
    std::string Environment() const {
        return environment_;
    }

    void DisableUpdateCheck() {
        update_check_disabled_ = true;
    }

    error SetSettingsUseIdleDetection(const bool use_idle_detection);

    error SetSettingsAutotrack(const bool value);

    error SetSettingsOpenEditorOnShortcut(const bool value);

    error SetSettingsMenubarTimer(const bool menubar_timer);

    error SetSettingsMenubarProject(const bool menubar_project);

    error SetSettingsDockIcon(const bool dock_icon);

    error SetSettingsOnTop(const bool on_top);

    error SetSettingsReminder(const bool reminder);

    error SetSettingsPomodoro(const bool pomodoro);

    error SetSettingsPomodoroBreak(const bool pomodoro_break);

    error SetSettingsStopEntryOnShutdownSleep(const bool stop_entry);

    error SetSettingsShowTouchBar(const bool show_touch_bar);

    error SetSettingsStartAutotrackerWithoutSuggestions(const bool start_autotracker_without_suggestions);

    error SetSettingsStartAutotrackerWhileTimerIsRunning(const bool start_autotracker_while_timer_is_running);

    error SetSettingsActiveTab(const uint8_t active_tab);

    error SetSettingsColorTheme(const uint8_t color_theme);

    error SetSettingsForceIgnoreCert(const bool_t force_ignore_cert);

    error SetSettingsIdleMinutes(const Poco::UInt64 idle_minutes);

    error SetSettingsFocusOnShortcut(const bool focus_on_shortcut);

    error SetSettingsReminderMinutes(const Poco::UInt64 reminder_minutes);

    error SetSettingsPomodoroMinutes(const Poco::UInt64 pomodoro_minutes);

    error SetSettingsPomodoroBreakMinutes(
        const Poco::UInt64 pomodoro_break_minutes);

    error SetSettingsManualMode(const bool manual_mode);

    error SetSettingsAutodetectProxy(const bool autodetect_proxy);

    error SetSettingsRemindTimes(
        const std::string &remind_starts,
        const std::string &remind_ends);

    error SetSettingsRemindDays(
        const bool remind_mon,
        const bool remind_tue,
        const bool remind_wed,
        const bool remind_thu,
        const bool remind_fri,
        const bool remind_sat,
        const bool remind_sun);

    void SetMiniTimerVisible(
        const bool);

    bool GetMiniTimerVisible();

    void SetKeepEndTimeFixed(
        const bool);

    bool GetKeepEndTimeFixed();

    bool GetShowTouchBar();

    uint8_t GetActiveTab();

    void SetWindowMaximized(
        const bool value);

    bool GetWindowMaximized();

    void SetWindowMinimized(
        const bool_t value);

    bool GetWindowMinimized();

    void SetWindowEditSizeHeight(
        const int64_t value);

    int64_t GetWindowEditSizeHeight();

    void SetWindowEditSizeWidth(
        const int64_t value);

    int64_t GetWindowEditSizeWidth();

    void SetMessageSeen(
        const int64_t value);

    int64_t GetMessageSeen();

    void SetKeyStart(
        const std::string &value);

    std::string GetKeyStart();

    void SetKeyShow(
        const std::string &value);

    std::string GetKeyShow();

    void SetKeyModifierShow(
        const std::string &value);

    std::string GetKeyModifierShow();

    void SetKeyModifierStart(
        const std::string &value);

    std::string GetKeyModifierStart();

    error ProxySettings(bool *use_proxy, Proxy *proxy);

    error SetProxySettings(const bool use_proxy,
                           const Proxy &proxy);

    error LoadWindowSettings(
        int64_t *window_x,
        int64_t *window_y,
        int64_t *window_height,
        int64_t *window_width);

    error SaveWindowSettings(
        const int64_t window_x,
        const int64_t window_y,
        const int64_t window_height,
        const int64_t window_width);

    Poco::Int64 GetMiniTimerX();

    void SetMiniTimerX(
        const int64_t x);

    Poco::Int64 GetMiniTimerY();

    void SetMiniTimerY(
        const int64_t y);

    Poco::Int64 GetMiniTimerW();

    void SetMiniTimerW(
        const int64_t w);

    error AsyncLogin(const std::string &email,
                     const std::string &password);

    error Login(
        const std::string &email,
        const std::string &password,
        const bool isSignup = false);

    error AsyncSignup(
        const std::string &email,
        const std::string &password,
        const uint64_t country_id);

    error Signup(
        const std::string &email,
        const std::string &password,
        const uint64_t country_id);

    error GoogleSignup(
        const std::string &access_token,
        const uint64_t country_id);

    error AsyncGoogleSignup(
        const std::string &access_token,
        const uint64_t country_id);

    error AppleSignup(
        const std::string &access_token,
        const uint64_t country_id,
        const std::string &full_name);

    error AsyncAppleSignup(
        const std::string &access_token,
        const uint64_t country_id,
        const std::string &full_name);

    error GoogleLogin(const std::string &access_token);
    error AsyncGoogleLogin(const std::string &access_token);

    error AppleLogin(const std::string &access_token);
    error AsyncAppleLogin(const std::string &access_token);

    error GetSSOIdentityProvider(const std::string &email);
    error EnableSSO(const std::string &code, const std::string &api_token);
    void LoginSSO(const std::string &api_token);
    void SetNeedEnableSSO(const std::string &code);
    void ResetEnableSSO();

    error Logout();

    error SetLoggedInUserFromJSON(
        const std::string &json,
        const bool isSignup = false);

    error ClearCache();

    TimeEntry *Start(
        const std::string &description,
        const std::string &duration,
        const Poco::UInt64 task_id,
        const Poco::UInt64 project_id,
        const std::string &project_guid,
        const std::string &tags,
        const bool prevent_on_app,
        const time_t started,
        const time_t ended,
        const bool stop_current_running);

    TimeEntry *ContinueLatest(const bool prevent_on_app);

    TimeEntry *Continue(
        const std::string &GUID);

    void OpenTimeEntryList();

    void OpenTimelineDataView();

    void ViewTimelinePrevDay();

    void ViewTimelineCurrentDay();

    void ViewTimelineNextDay();

    void ViewTimelineSetDate(const Poco::Int64 unix_timestamp);

    void OpenSettings();

    void OpenTimeEntryEditor(
        const std::string &GUID,
        const bool edit_running_entry = false,
        const std::string &focused_field_name = "");

    error SetTimeEntryDuration(
        const std::string &GUID,
        const std::string &duration);

    error DeleteTimeEntryByGUID(const std::string &GUID);

    error SetTimeEntryProject(
        const std::string &GUID,
        const Poco::UInt64 task_id,
        const Poco::UInt64 project_id,
        const std::string &project_guid);

    error UpdateTimeEntry(
        const std::string &GUID,
        const std::string &description,
        const Poco::UInt64 task_id,
        const Poco::UInt64 project_id,
        const std::string &project_guid,
        const std::string &tags,
        const bool billable);

    error SetTimeEntryDate(
        const std::string &GUID,
        const Poco::Int64 unix_timestamp);

    error SetTimeEntryStart(
        const std::string &GUID,
        const std::string &value);

    error SetTimeEntryStart(const std::string &GUID,
                            const Poco::Int64 startAt);

    error SetTimeEntryStartWithOption(const std::string &GUID,
                                      const Poco::Int64 startAt,
                                      const bool keepEndTimeFixed);

    error SetTimeEntryStop(
        const std::string &GUID,
        const std::string &value);

    error SetTimeEntryStop(const std::string &GUID,
                           const Poco::Int64 endAt);

    error SetTimeEntryTags(
        const std::string &GUID,
        const std::string &value);

    error SetTimeEntryBillable(
        const std::string &GUID,
        const bool value);

    error SetTimeEntryDescription(
        const std::string &GUID,
        const std::string &value);

    error Stop(const bool prevent_on_app);

    error DiscardTimeAt(
        const std::string &GUID,
        const Poco::Int64 at,
        const bool split_into_new_entry);

    TimeEntry *DiscardTimeAndContinue(
        const std::string &GUID,
        const Poco::Int64 at);

    TimeEntry *RunningTimeEntry();

    bool CanSeeBillable(const Poco::UInt64 workspaceID) const;

    void FetchTags(const Poco::UInt64 workspaceID);

    error ToggleTimelineRecording(
        const bool record_timeline);

    bool IsTimelineRecordingEnabled() const {
        return user_ && user_->RecordTimeline();
    }

    std::string GetTimeOfDayFormat() const {
        return user_ ? user_->TimeOfDayFormat() : "";
    }

    error SetDefaultProject(
        const Poco::UInt64 pid,
        const Poco::UInt64 tid);
    error DefaultProjectName(std::string *name);
    error DefaultPID(Poco::UInt64 *result);
    error DefaultTID(Poco::UInt64 *result);
    error DefaultOrFirstWID(Poco::UInt64 *result);

    void SearchHelpArticles(
        const std::string &keywords);

    error SetUpdateChannel(
        const std::string &channel);

    error UpdateChannel(
        std::string *update_channel);

    Project *CreateProject(
        const Poco::UInt64 workspace_id,
        const Poco::UInt64 client_id,
        const std::string &client_guid,
        const std::string &project_name,
        const bool is_private,
        const std::string &project_color);

    Client *CreateClient(
        const Poco::UInt64 workspace_id,
        const std::string &client_name);

    void SetSleep();

    void SetWake();

    void SetLocked();

    void SetUnlocked();

    void osShutdown();

    void SetOnline();

    error AsyncOpenReportsInBrowser();
    error OpenReportsInBrowser();

    error ToSAccept();

    void SetIdleSeconds(const Poco::UInt64 idle_seconds) {
        idle_.SetIdleSeconds(idle_seconds, user_);
    }

    void LoadMore();

    static void SetLogPath(const std::string &path);

    void SetQuit() {
        quit_ = true;
    }

    error AddAutotrackerRule(
        const std::string &terms,
        const Poco::UInt64 pid,
        const Poco::UInt64 tid,
        const std::string &start_time,
        const std::string &end_time,
        const Poco::UInt8 days_of_week,
        Poco::Int64 *rule_id);

    error UpdateAutotrackerRule(
        const Poco::Int64 rule_id,
        const std::string &terms,
        const Poco::UInt64 pid,
        const Poco::UInt64 tid,
        const std::string &start_time,
        const std::string &end_time,
        const Poco::UInt8 days_of_week);

    error DeleteAutotrackerRule(
        const Poco::Int64 id);

    std::string UserFullName();
    std::string UserEmail();
    Poco::UInt8 UserBeginningOfWeek();

    // Timeline datasource
    error StartAutotrackerEvent(const TimelineEvent &event) override;
    error CreateCompressedTimelineBatchForUpload(TimelineBatch *batch) override;
    error StartTimelineEvent(TimelineEvent *event) override;
    error MarkTimelineBatchAsUploaded(
        const std::vector<const TimelineEvent*> &events) override;

    error SetPromotionResponse(
        const int64_t promotion_type,
        const int64_t promotion_response);

    error ToggleEntriesGroup(
        std::string name);

    error AsyncPullCountries();
    error PullCountries();

    void TrackWindowSize(const Poco::UInt64 width,
                         const Poco::UInt64 height);

    void TrackEditSize(const Poco::Int64 width,
                       const Poco::Int64 height);

    void TrackInAppMessage(const Poco::Int64 type);

    void TrackCollapseDay();

    void TrackExpandDay();

    void TrackCollapseAllDays();

    void TrackExpandAllDays();

    void TrackTimerEdit(TimerEditActionType action);
    void TrackTimerStart(TimerEditActionType actions);
    void TrackTimerShortcut(TimerShortcutActionType action);
    void TrackDurationDropdown(DurationDropdownActionType action);

    // Onboarding action
    void UserDidClickOnTimelineTab();
    void UserDidTurnOnRecordActivity();
    void UserDidEditOrAddTimeEntryOnTimelineView();

    void TrackTimelineMenuContext(const TimelineMenuContextType type);

    AlphaFeatures* GetAlphaFeatures() {
        return user_ ? user_->AlphaFeatureSettings : nullptr;
    }

 protected:
    void uiUpdaterActivity();
    void checkReminders();
    void reminderActivity();
    void syncerActivityWrapper();
    void legacySyncerActivity();
    void batchedSyncerActivity();

 private:
    static const std::string installerPlatform();
    static const std::string linuxPlatformName();
    static const std::string windowsPlatformName();
    static const std::string shortOSName();

    static void parseVersion(int result[4], const std::string& input);
    static bool lessThanVersion(const std::string& version1, const std::string& version2);

    Logger logger { "context" };

    void sync(const bool full_sync);

    error save(const bool push_changes = true);

    void fetchUpdates();

    // timer_ callbacks
    void onSwitchWebSocketOff(Poco::Util::TimerTask& task);  // NOLINT
    void onSwitchWebSocketOn(Poco::Util::TimerTask& task);  // NOLINT
    void onSwitchTimelineOff(Poco::Util::TimerTask& task);  // NOLINT
    void onSwitchTimelineOn(Poco::Util::TimerTask& task);  // NOLINT
    void onFetchUpdates(Poco::Util::TimerTask& task);  // NOLINT
    void onPeriodicUpdateCheck(Poco::Util::TimerTask& task);  // NOLINT
    void onPeriodicInAppMessageCheck(Poco::Util::TimerTask& task);  // NOLINT
    void onTimelineUpdateServerSettings(Poco::Util::TimerTask& task);  // NOLINT
    void onSendFeedback(Poco::Util::TimerTask& task);  // NOLINT
    void onPeriodicSync(Poco::Util::TimerTask& task);  // NOLINT
    void onTrackSettingsUsage(Poco::Util::TimerTask& task);  // NOLINT
    void onWake(Poco::Util::TimerTask& task);  // NOLINT
    void onLoadMore(Poco::Util::TimerTask& task); // NOLINT

    void onTimeEntryAutocompletes(Poco::Util::TimerTask& task);  // NOLINT
    void onMiniTimerAutocompletes(Poco::Util::TimerTask& task);  // NOLINT
    void onProjectAutocompletes(Poco::Util::TimerTask& task);  // NOLINT

    void startPeriodicUpdateCheck();
    void executeUpdateCheck();

    void startPeriodicInAppMessageCheck();

    void startPeriodicSync();

    void setUser(User *value, const bool user_logged_in = false);

    void switchWebSocketOff();
    void switchWebSocketOn();
    void switchTimelineOff();
    void switchTimelineOn();

    Database *db() const;

    void displayTimeEntryEditor(const bool open,
                                TimeEntry *te,
                                const std::string &focused_field_name);
    void displayReminder();
    void resetLastTrackingReminderTime();

    void displayPomodoro();

    void displayPomodoroBreak();

    void updateUI(const UIElements &elements);

    error displayError(const error &err);

    void scheduleSync();

    void setOnline(const std::string &reason);

    int nextSyncIntervalSeconds() const;

    bool isPostponed(
        const Poco::Timestamp value,
        const Poco::Timestamp::TimeDiff throttleMicros) const;

    Poco::Timestamp postpone(
        const Poco::Timestamp::TimeDiff throttleMicros) const;

    error attemptOfflineLogin(const std::string &email,
                              const std::string &password);

    error downloadUpdate();

    error fetchMessage(const bool periodic);

    void stopActivities();

    error offerBetaChannel(bool *did_offer);

    error compressTimeline();

    error applySettingsSaveResultToUI(const error &err);

    error pullAllUserData();
    error pullBatchedUserData();
    error pullChanges();
    error pullUserPreferences();
    error pullAllPreferencesData();

    template <typename T>
    void syncCollectJSON(Json::Value &array, const std::vector<T*> &source);
    void syncStripPremiumDataFromModelJSON(Json::Value &item);
    bool syncTranslateGUIDToLocalID(Json::Value &item);
    template <typename T>
    error syncHandleResponse(Json::Value &array, const std::vector<T*> &source);

    error pushBatchedChanges(
        bool *had_something_to_push);
    error pushChanges(
        bool *had_something_to_push);
    error pushClients(
        const std::vector<Client *> &clients,
        const std::string &api_token);
    error pushProjects(
        const std::vector<Project *> &projects,
        const std::vector<Client *> &clients,
        const std::string &api_token);
    error pushEntries(
        const std::map<std::string, BaseModel *> &models,
        const std::vector<TimeEntry *> &time_entries,
        const std::string &api_token);
    error updateProjectClients(
        const std::vector<Client *> &clients,
        const std::vector<Project *> &projects);
    error updateEntryProjects(
        const std::vector<Project *> &projects,
        const std::vector<TimeEntry *> &time_entries);
    error signup(
        const std::string &email,
        const std::string &password,
        std::string *user_data_json,
        const uint64_t country_id);
    error signupGoogle(
        const std::string &access_token,
        std::string *user_data_json,
        const uint64_t country_id);
    error signupApple(
        const std::string &access_token,
        std::string *user_data_json,
        const std::string &full_name,
        const uint64_t country_id);
    error signUpWithProvider(
        const std::string &access_token,
        std::string *user_data_json,
        const uint64_t country_id,
        const std::string &full_name,
        const std::string &provider);

    static error me(const std::string &email,
                    const std::string &password,
                    std::string *user_data,
                    const Poco::Int64 since);
    static error syncPull(const std::string &email,
                          const std::string &password,
                          std::string *user_data,
                          const Poco::Int64 since);

    bool isTimeEntryLocked(TimeEntry* te);
    bool isTimeLockedInWorkspace(time_t t, Workspace* ws);
    bool canChangeStartTimeTo(TimeEntry* te, time_t t);
    bool canChangeProjectTo(TimeEntry* te, Project* p);

    error logAndDisplayUserTriedEditingLockedEntry();

    error runGetRequest(const std::string &endpoint, const std::function<void(const std::string)> handler);
    error pullTimeEntries();
    error pullProjects();
    error pullWorkspaces();
    error pullClients();
    error pullTasks();
    error pullTags();
    error pullInitialObjects();

    error pullWorkspacePreferences();
    error pullWorkspacePreferences(Workspace *workspace, std::string* json);

    template<typename T>
    void collectPushableModels(
        const std::vector<T *> &list,
        std::vector<T *> *result,
        std::map<std::string, BaseModel *> *models = nullptr) const;

    Poco::Mutex db_m_;
    Database *db_;

    Poco::Mutex user_m_;
    User *user_;

    Poco::Mutex ws_client_m_;
    WebSocketClient ws_client_;

    Poco::Mutex timeline_uploader_m_;
    TimelineUploader *timeline_uploader_;

    Poco::Mutex window_change_recorder_m_;
    WindowChangeRecorder *window_change_recorder_;

    custom_error_handler error_handler_;

    Feedback feedback_;

    // Tasks are scheduled at:
    Poco::Timestamp next_sync_at_;
    Poco::Timestamp next_push_changes_at_;
    Poco::Timestamp next_fetch_updates_at_;
    Poco::Timestamp next_update_timeline_settings_at_;
    Poco::Timestamp next_wake_at_;

    // Schedule tasks using a timer:
    Poco::Mutex timer_m_;
    Poco::Util::Timer timer_;

    class GUI ui_;

    std::string time_entry_editor_guid_;

    std::string environment_;

    Idle idle_;

    Poco::Int64 last_sync_started_;
    Poco::Int64 sync_interval_seconds_;
    Poco::Int64 last_tracking_reminder_time_;
    Poco::Int64 last_pomodoro_reminder_time_;
    Poco::Int64 last_pomodoro_break_reminder_time_;

    bool update_check_disabled_;

    bool trigger_sync_;
    bool trigger_push_;
    bool trigger_full_sync_;

    Poco::LocalDateTime last_time_entry_list_render_at_;

    bool quit_;

    Poco::Mutex ui_updater_m_;
    Poco::Activity<Context> ui_updater_;

    Poco::Mutex reminder_m_;
    Poco::Activity<Context> reminder_;

    Poco::Mutex syncer_m_;
    Poco::Activity<Context> syncer_;
    std::string lastRequestUUID_;

    Analytics analytics_;

    std::string update_path_;

    static std::string log_path_;

    Settings settings_;

    std::set<std::string> autotracker_titles_;

    HelpDatabase help_database_;

    TimeEntry *pomodoro_break_entry_;

    // To cache grouped entries open/close status
    std::map<std::string, bool_t> entry_groups;

    bool overlay_visible_;

    std::string last_message_id_;

    bool handleStopRunningEntry();

    error updateTimeEntryProject(
        TimeEntry *te,
        const Poco::UInt64 task_id,
        const Poco::UInt64 project_id,
        const std::string &project_guid);

    error updateTimeEntryDescription(
        TimeEntry *te,
        const std::string &value);

    Poco::Mutex onboarding_service_m_;

    bool checkIfSkipPomodoro(TimeEntry *te);

    bool isUsingSyncServer() const;

    bool need_enable_SSO;
    std::string sso_confirmation_code;

    enum SyncState {
        STARTUP,
        LEGACY,
        BATCHED
    } sync_state_;
};
void on_websocket_message(
    void *context,
    std::string json);

}  // namespace toggl

#endif  // SRC_CONTEXT_H_
