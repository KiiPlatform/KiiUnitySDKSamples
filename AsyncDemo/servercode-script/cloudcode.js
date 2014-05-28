function sum(params, context, done){
  Kii.initializeWithSite(context.headers["X-Kii-AppID"], context.headers["X-Kii-AppKey"], params["baseUrl"]);
  var uri = params["groupUri"];
  var name = params["bucketName"];
  var token = context.getAccessToken();
  KiiUser.authenticateWithToken(token, {
      success: function(theAuthedUser) {
          var group = KiiGroup.groupWithURI(uri);
          var bucket = group.bucketWithName(name);
          var query = KiiQuery.queryWithClause();
          bucket.executeQuery(query, {
            success: function(queryPerformed, resultSet, nextQuery) {
              var totalScore = 0;
              for (var i = 0; i < resultSet.length; i++) {
                var result = resultSet[i]['_customInfo'];
                totalScore = totalScore+result['score'];
              }
              done({total_score : totalScore});
            },
            failure: function(queryPerformed, errorString) {
              done(errorString);
            }
          });
      },
      failure: function(theUser, anErrorString) {
          done(anErrorString);
      }
  });
}

