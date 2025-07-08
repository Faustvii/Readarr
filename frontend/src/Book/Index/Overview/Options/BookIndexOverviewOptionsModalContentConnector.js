import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setAuthorOverviewOption } from 'Store/Actions/authorIndexActions';
import BookIndexOverviewOptionsModalContent from './BookIndexOverviewOptionsModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.authorIndex,
    (authorIndex) => {
      const overviewOptions = authorIndex.overviewOptions || {};
      return {
        size: overviewOptions.size || 'medium',
        detailedProgressBar: overviewOptions.detailedProgressBar ?? false,
        showReleaseDate: overviewOptions.showReleaseDate ?? true,
        showMonitored: overviewOptions.showMonitored ?? true,
        showQualityProfile: overviewOptions.showQualityProfile ?? true,
        showAdded: overviewOptions.showAdded ?? true,
        showPath: overviewOptions.showPath ?? true,
        showSizeOnDisk: overviewOptions.showSizeOnDisk ?? true,
        showSearchAction: overviewOptions.showSearchAction ?? true
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onChangeOverviewOption(payload) {
      dispatch(setAuthorOverviewOption(payload));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(BookIndexOverviewOptionsModalContent);
