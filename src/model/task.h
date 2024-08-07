// Copyright 2014 Toggl Desktop developers.

#ifndef SRC_TASK_H_
#define SRC_TASK_H_

#include <string>

#include <json/json.h>  // NOLINT

#include <Poco/Types.h>

#include "model/base_model.h"

namespace toggl {

class TOGGL_INTERNAL_EXPORT Task : public BaseModel {
 public:
    Task() : BaseModel() {}
    // Before undeleting, see how the copy constructor in BaseModel works
    Task(const Task &o) = delete;
    Task &operator=(const Task &o) = delete;

    Property<std::string> Name { "" };
    Property<Poco::UInt64> WID { 0 };
    Property<Poco::UInt64> PID { 0 };
    Property<bool> Active { false };

    void SetName(const std::string &value);
    void SetWID(Poco::UInt64 value);
    void SetPID(Poco::UInt64 value);
    void SetActive(bool value);

    // Override BaseModel
    std::string String() const override;
    std::string ModelName() const override;
    std::string ModelURL() const override;
    void LoadFromJSON(const Json::Value &value);
};

}  // namespace toggl

#endif  // SRC_TASK_H_
